// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

// @diogo: preview storage as slices of chunked Tex2DArray render textures instead of one RenderTexture
// per port. Creating a render texture costs ~0.3 ms on DX12 ( ~0.1 ms on DX11 ) regardless of its size,
// so a fresh load of a huge graph spent seconds allocating; one array chunk costs the same as one texture
// and holds dozens of slices. Previews render straight into their slice and preview shaders sample their
// ports as arrays ( ASE_PORT in Preview.cginc ). A texture cannot be sampled while it is the active render
// target - Unity binds black instead - so a node's output slice is allocated in a chunk none of its inputs
// live in ( Allocate's exclusion list ), and a bind that still collides ( rewired later ) falls back to a
// scratch copy ( a one-slice array for array ports, see GetScratchArray ); the few ports still declared
// sampler2D get a 2D one ( see GetScratch ).

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AmplifyShaderEditor
{
	// One preview's storage: a slice of an array chunk. Default value means "no storage".
	public struct ASEPreviewSlice
	{
		public RenderTexture Array;
		public int Slice;

		public bool IsValid { get { return Array != null; } }
		public int Size { get { return Array != null ? Array.width : 0; } }
		public RenderTextureFormat Format { get { return Array != null ? Array.format : RenderTextureFormat.ARGBHalf; } }
		public long Bytes { get { return ( long )Size * Size * ASEPreviewSlices.BytesPerPixel( Format ); } }

		public void Blit( Material material, int pass )
		{
			Graphics.Blit( null, Array, material, pass, Slice );
		}

		public void CopyFrom( ASEPreviewSlice source )
		{
			Graphics.CopyTexture( source.Array, source.Slice, 0, Array, Slice, 0 );
		}

		// Copies this slice into a 2D texture of the same size/format.
		public void CopyTo( RenderTexture destination )
		{
			Graphics.CopyTexture( Array, Slice, 0, destination, 0, 0 );
		}

		// @diogo: a sampler2D can't address an array slice, so binding to one goes through a 2D scratch copy; the
		// scratch index keeps the inputs of one render on distinct scratches ( see ASEPreviewSlices.GetScratch ).
		public RenderTexture To2D( int scratchIndex )
		{
			RenderTexture scratch = ASEPreviewSlices.GetScratch( Size, Format, scratchIndex );
			CopyTo( scratch );
			return scratch;
		}

		public void Bind( Material material, int propertyId, int scratchIndex )
		{
			Bind( material, propertyId, scratchIndex, null );
		}

		// @diogo: a port declared as 2DArray ( ASE_PORT in Preview.cginc ) takes the array and slice directly,
		// unless the consumer renders into this same array ( see the file header ), where it gets a one-slice
		// array scratch copy; a plain sampler2D port ( mixed-source nodes like Triplanar ) gets the 2D scratch.
		public void Bind( Material material, int propertyId, int scratchIndex, ParentNode consumer )
		{
			ASEPreviewSlices.PortProperty port = ASEPreviewSlices.FindPort( material, propertyId );
			if ( port != null && port.IsArray )
			{
				if ( consumer == null || !consumer.UsesPreviewArray( Array ) )
				{
					material.SetTexture( propertyId, Array );
					material.SetFloat( port.SliceId, Slice );
				}
				else
				{
					RenderTexture scratch = ASEPreviewSlices.GetScratchArray( Size, Format, scratchIndex );
					Graphics.CopyTexture( Array, Slice, 0, scratch, 0, 0 );
					material.SetTexture( propertyId, scratch );
					material.SetFloat( port.SliceId, 0 );
				}
			}
			else
			{
				material.SetTexture( propertyId, To2D( scratchIndex ) );
			}
		}
	}

	[NoAutoStaticsCleanup]
	public static class ASEPreviewSlices
	{
		// @diogo: chunk VRAM target; slices per chunk derive from it. Small enough that keeping a node's outputs
		// out of its inputs' chunks costs little idle VRAM, large enough that a big graph needs few allocations.
		private const long ChunkTargetBytes = 4 * 1024 * 1024;
		private const int MinSlicesPerChunk = 16;
		private const int MaxSlicesPerChunk = 1024;

		// Scratch indices above the input-port range, for the out-of-band readers ( masking passes, reference
		// registry reads, picker readback, inspector thumbnails ), so they never alias a wired input's scratch.
		// Reference and Display readers may add small offsets, hence the gaps.
		public const int MaskScratchIndex = 128;
		public const int ReferenceScratchIndex = 129;
		public const int PickerScratchIndex = 145;
		public const int DisplayScratchIndex = 146;

		private class Chunk
		{
			public RenderTexture Array;
			public readonly Stack<int> Free = new Stack<int>();
			public int Live;
		}

		private class Pool
		{
			public int Size;
			public RenderTextureFormat Format;
			public int SlicesPerChunk;
			public readonly List<Chunk> Chunks = new List<Chunk>();
			public readonly List<RenderTexture> Scratch = new List<RenderTexture>();
			public readonly List<RenderTexture> ScratchArrays = new List<RenderTexture>();
			public int Live;
		}

		// A port texture property ( _A.._Z ) of a preview shader, resolved once per shader.
		public class PortProperty
		{
			public int PortIndex;
			public int Id;
			public int SliceId;
			public bool IsArray;
			public bool DefaultWhite;
		}

		private static readonly Dictionary<long, Pool> s_pools = new Dictionary<long, Pool>();
		private static readonly Dictionary<Shader, PortProperty[]> s_shaderPorts = new Dictionary<Shader, PortProperty[]>();
		private static readonly List<RenderTexture> s_excluded = new List<RenderTexture>();
		private static readonly PortProperty[] s_noPorts = new PortProperty[ 0 ];

		// @diogo: white and black live in a chunk of their own, so a node that reads them can never be
		// rendering into their array.
		private static RenderTexture s_constantArray;

		public static int LiveSlices
		{
			get
			{
				int live = 0;
				foreach ( Pool pool in s_pools.Values )
				{
					live += pool.Live;
				}
				return live;
			}
		}

		public static long AllocatedBytes
		{
			get
			{
				long bytes = 0;
				foreach ( Pool pool in s_pools.Values )
				{
					long sliceBytes = ( long )pool.Size * pool.Size * BytesPerPixel( pool.Format );
					bytes += sliceBytes * pool.SlicesPerChunk * pool.Chunks.Count;
					for ( int i = 0; i < pool.Scratch.Count; i++ )
					{
						if ( pool.Scratch[ i ] != null )
						{
							bytes += sliceBytes;
						}
					}
					for ( int i = 0; i < pool.ScratchArrays.Count; i++ )
					{
						if ( pool.ScratchArrays[ i ] != null )
						{
							bytes += sliceBytes;
						}
					}
				}
				return bytes;
			}
		}

		public static long BytesPerPixel( RenderTextureFormat format )
		{
			switch ( format )
			{
				case RenderTextureFormat.ARGBFloat: return 16;
				case RenderTextureFormat.ARGBHalf: return 8;
				case RenderTextureFormat.ARGB32:
				case RenderTextureFormat.Default: return 4;
				default: return 8;
			}
		}

		public static ASEPreviewSlice Allocate( int size, RenderTextureFormat format )
		{
			return Allocate( size, format, null );
		}

		// Allocates a slice for a consumer's output, outside the chunks the consumer's inputs are read from.
		public static ASEPreviewSlice Allocate( int size, RenderTextureFormat format, ParentNode consumer )
		{
			s_excluded.Clear();
			if ( consumer != null )
			{
				consumer.CollectInputPreviewArrays( s_excluded );
			}

			// @diogo: newest chunks first - the older ones fill up, so a forward scan would walk them every time.
			Pool pool = GetPool( size, format );
			Chunk owner = null;
			for ( int i = pool.Chunks.Count - 1; i >= 0; i-- )
			{
				Chunk chunk = pool.Chunks[ i ];
				if ( chunk.Free.Count > 0 && !s_excluded.Contains( chunk.Array ) )
				{
					owner = chunk;
					break;
				}
			}

			if ( owner == null )
			{
				owner = new Chunk();
				owner.Array = CreateArray( size, format, pool.SlicesPerChunk );
				for ( int i = pool.SlicesPerChunk - 1; i >= 0; i-- )
				{
					owner.Free.Push( i );
				}
				pool.Chunks.Add( owner );
			}

			owner.Live++;
			pool.Live++;

			ASEPreviewSlice slice;
			slice.Array = owner.Array;
			slice.Slice = owner.Free.Pop();
			return slice;
		}

		public static void Free( ASEPreviewSlice slice )
		{
			if ( !slice.IsValid )
			{
				return;
			}

			Pool pool;
			if ( !s_pools.TryGetValue( Key( slice.Size, slice.Format ), out pool ) )
			{
				return;
			}

			for ( int i = 0; i < pool.Chunks.Count; i++ )
			{
				Chunk chunk = pool.Chunks[ i ];
				if ( chunk.Array == slice.Array )
				{
					chunk.Live--;
					pool.Live--;

					// @diogo: a chunk that emptied at a size no longer in use ( zoom band change, quality change )
					// gives its VRAM back; the current size keeps its chunks warm for the next pass.
					if ( chunk.Live == 0 && pool.Size != UIUtils.CurrentPreviewSize && pool.Size != 1 )
					{
						Destroy( chunk.Array );
						pool.Chunks.RemoveAt( i );
					}
					else
					{
						chunk.Free.Push( slice.Slice );
					}
					return;
				}
			}
		}

		// Persistent 2D texture at the given size/format for binding or drawing slice contents.
		public static RenderTexture GetScratch( int size, RenderTextureFormat format, int index )
		{
			Pool pool = GetPool( size, format );
			while ( pool.Scratch.Count <= index )
			{
				pool.Scratch.Add( null );
			}
			if ( pool.Scratch[ index ] == null )
			{
				RenderTexture scratch = new RenderTexture( size, size, 0, format, RenderTextureReadWrite.Linear );
				scratch.wrapMode = TextureWrapMode.Repeat;
				scratch.Create();
				pool.Scratch[ index ] = scratch;
			}
			return pool.Scratch[ index ];
		}

		// Persistent one-slice array at the given size/format, for binding a slice to an array port that collides.
		public static RenderTexture GetScratchArray( int size, RenderTextureFormat format, int index )
		{
			Pool pool = GetPool( size, format );
			while ( pool.ScratchArrays.Count <= index )
			{
				pool.ScratchArrays.Add( null );
			}
			if ( pool.ScratchArrays[ index ] == null )
			{
				pool.ScratchArrays[ index ] = CreateArray( size, format, 1 );
			}
			return pool.ScratchArrays[ index ];
		}

		public static PortProperty[] GetShaderPorts( Shader shader )
		{
			PortProperty[] ports;
			if ( shader == null )
			{
				return s_noPorts;
			}

			if ( !s_shaderPorts.TryGetValue( shader, out ports ) )
			{
				List<PortProperty> found = new List<PortProperty>();
				int count = shader.GetPropertyCount();
				for ( int i = 0; i < count; i++ )
				{
					string name = shader.GetPropertyName( i );
					if ( shader.GetPropertyType( i ) == ShaderPropertyType.Texture && name.Length == 2 && name[ 0 ] == '_' && name[ 1 ] >= 'A' && name[ 1 ] <= 'Z' )
					{
						PortProperty port = new PortProperty();
						port.PortIndex = name[ 1 ] - 'A';
						port.Id = Shader.PropertyToID( name );
						port.SliceId = Shader.PropertyToID( name + "_Slice" );
						port.IsArray = shader.GetPropertyTextureDimension( i ) == TextureDimension.Tex2DArray;
						port.DefaultWhite = shader.GetPropertyTextureDefaultName( i ) == "white";
						found.Add( port );
					}
				}
				ports = found.ToArray();
				s_shaderPorts.Add( shader, ports );
			}
			return ports;
		}

		public static PortProperty FindPort( Material material, int propertyId )
		{
			PortProperty[] ports = GetShaderPorts( material.shader );
			for ( int i = 0; i < ports.Length; i++ )
			{
				if ( ports[ i ].Id == propertyId )
				{
					return ports[ i ];
				}
			}
			return null;
		}

		public static bool IsArrayPort( Material material, int propertyId )
		{
			PortProperty port = FindPort( material, propertyId );
			return port != null && port.IsArray;
		}

		// Binds a constant white or black texel to a port, in whichever form the shader declares the port.
		public static void BindConstant( Material material, int propertyId, bool white )
		{
			PortProperty port = FindPort( material, propertyId );
			if ( port != null && port.IsArray )
			{
				material.SetTexture( propertyId, ConstantArray );
				material.SetFloat( port.SliceId, white ? 0 : 1 );
			}
			else
			{
				material.SetTexture( propertyId, white ? Texture2D.whiteTexture : Texture2D.blackTexture );
			}
		}

		// @diogo: preview shaders declare the maximum port count and dynamic-port nodes use fewer; an unbound
		// sampler2D read its Properties default, which an array port has no built-in equivalent of, so the
		// declared default ( white or black ) is bound explicitly to every port past the node's input count.
		public static void BindUnusedPorts( Material material, int inputCount )
		{
			PortProperty[] ports = GetShaderPorts( material.shader );
			for ( int i = 0; i < ports.Length; i++ )
			{
				if ( ports[ i ].PortIndex >= inputCount )
				{
					BindConstant( material, ports[ i ].Id, ports[ i ].DefaultWhite );
				}
			}
		}

		public static void FlushAll()
		{
			Destroy( s_constantArray );
			s_constantArray = null;
			foreach ( Pool pool in s_pools.Values )
			{
				for ( int i = 0; i < pool.Chunks.Count; i++ )
				{
					Destroy( pool.Chunks[ i ].Array );
				}
				for ( int i = 0; i < pool.Scratch.Count; i++ )
				{
					Destroy( pool.Scratch[ i ] );
				}
				for ( int i = 0; i < pool.ScratchArrays.Count; i++ )
				{
					Destroy( pool.ScratchArrays[ i ] );
				}
			}
			s_pools.Clear();
		}

		// A two-slice array holding a white texel at slice 0 and a black one at slice 1.
		private static RenderTexture ConstantArray
		{
			get
			{
				if ( s_constantArray == null )
				{
					s_constantArray = CreateArray( 1, Preferences.User.PreviewFormat, 2 );
					Graphics.Blit( Texture2D.whiteTexture, s_constantArray, 0, 0 );
					Graphics.Blit( Texture2D.blackTexture, s_constantArray, 0, 1 );
				}
				return s_constantArray;
			}
		}

		private static RenderTexture CreateArray( int size, RenderTextureFormat format, int slices )
		{
			RenderTexture array = new RenderTexture( size, size, 0, format, RenderTextureReadWrite.Linear );
			array.dimension = TextureDimension.Tex2DArray;
			array.volumeDepth = slices;
			array.wrapMode = TextureWrapMode.Repeat;
			array.Create();
			return array;
		}

		private static void Destroy( RenderTexture texture )
		{
			if ( texture != null )
			{
				texture.Release();
				Object.DestroyImmediate( texture );
			}
		}

		private static Pool GetPool( int size, RenderTextureFormat format )
		{
			Pool pool;
			long key = Key( size, format );
			if ( !s_pools.TryGetValue( key, out pool ) )
			{
				pool = new Pool();
				pool.Size = size;
				pool.Format = format;
				long sliceBytes = ( long )size * size * BytesPerPixel( format );
				pool.SlicesPerChunk = ( int )Mathf.Clamp( ChunkTargetBytes / sliceBytes, MinSlicesPerChunk, MaxSlicesPerChunk );
				s_pools.Add( key, pool );
			}
			return pool;
		}

		private static long Key( int size, RenderTextureFormat format )
		{
			return ( ( long )size << 32 ) | ( uint )format;
		}
	}
}
