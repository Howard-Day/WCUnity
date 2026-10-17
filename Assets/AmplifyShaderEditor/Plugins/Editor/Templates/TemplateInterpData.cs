// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateInterpElement
	{
		public TemplateSemantics Semantic;
		public bool[] AvailableChannels = { true, true, true, true };
		public bool IsFull = false;
		public int Usage = 0;
		public string Name;
		//https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct
		public bool NoInterpolation;
		public bool Sample;

		// OR of the conditions of every value packed here; null when the slot is unconditional
		[NonSerialized]
		public InterpolatorCondition Condition;

		// Struct field line held back until Condition is final, null once emitted or when emitted eagerly
		[NonSerialized]
		public string DeferredDeclaration;

		public TemplateInterpElement( TemplateInterpElement other )
		{
			Semantic = other.Semantic;
			for ( int i = 0; i < AvailableChannels.Length; i++ )
			{
				AvailableChannels[ i ] = other.AvailableChannels[ i ];
			}
			IsFull = other.IsFull;
			Usage = other.Usage;
			Name = other.Name;
			NoInterpolation = other.NoInterpolation;
			Sample = other.Sample;
			Condition = other.Condition;
			DeferredDeclaration = other.DeferredDeclaration;
		}

		public TemplateInterpElement( TemplateSemantics semantic )
		{
			Semantic = semantic;
			int semanticId = TemplateHelperFunctions.SemanticToInt[ Semantic ];
			Name = ( semanticId == 0 ) ? TemplateHelperFunctions.BaseInterpolatorName : TemplateHelperFunctions.BaseInterpolatorName + semanticId.ToString();
		}

		public void SetAvailableChannelsFromString( string channels )
		{
			for ( int i = 0; i < AvailableChannels.Length; i++ )
			{
				AvailableChannels[ i ] = false;
			}
			Usage = AvailableChannels.Length;

			for ( int i = 0; i < channels.Length; i++ )
			{
				switch ( channels[ i ] )
				{
					case 'x': if ( !AvailableChannels[ 0 ] ) { AvailableChannels[ 0 ] = true; Usage--; } break;
					case 'y': if ( !AvailableChannels[ 1 ] ) { AvailableChannels[ 1 ] = true; Usage--; } break;
					case 'z': if ( !AvailableChannels[ 2 ] ) { AvailableChannels[ 2 ] = true; Usage--; } break;
					case 'w': if ( !AvailableChannels[ 3 ] ) { AvailableChannels[ 3 ] = true; Usage--; } break;
				}
			}
		}

		public TemplateVertexData RequestChannels( WirePortDataType type, bool isColor, string customName = null )
		{
			if ( IsFull )
				return null;

			int channelsRequired = TemplateHelperFunctions.DataTypeChannelUsage[ type ];
			if ( channelsRequired == 0 )
				return null;

			int firstChannel = -1;
			for ( int i = 0; i < AvailableChannels.Length; i++ )
			{
				if ( AvailableChannels[ i ] )
				{
					if ( firstChannel < 0 )
					{
						firstChannel = i;
					}
					channelsRequired -= 1;
					if ( channelsRequired == 0 )
						break;
				}
			}

			//did not found enough channels to fill request
			if ( channelsRequired > 0 )
				return null;

			if( Usage == 0 && customName != null )
			{
				Name = customName;
			}

			Usage += 1;
			TemplateVertexData data = null;

			if ( type == WirePortDataType.COLOR || type == WirePortDataType.FLOAT4 )
			{
				// Automatically lock all channels
				for ( int i = firstChannel; i < ( firstChannel + channelsRequired ); i++ )
				{
					AvailableChannels[ i ] = false;
				}
				IsFull = true;
				data = new TemplateVertexData( Semantic, type, Name );
			}
			else
			{
				string[] swizzleArray = ( isColor ) ? TemplateHelperFunctions.ColorSwizzle : TemplateHelperFunctions.VectorSwizzle;
				string channels = ".";
				int count = firstChannel + TemplateHelperFunctions.DataTypeChannelUsage[ type ];
				for ( int i = firstChannel; i < count; i++ )
				{
					AvailableChannels[ i ] = false;
					channels += swizzleArray[ i ];
					if ( i == ( AvailableChannels.Length - 1 ) )
					{
						IsFull = true;
					}
				}

				data = new TemplateVertexData( Semantic, type, Name, channels );
			}
			return data;
		}
	}

	// Preprocessor condition of a keyword-gated interpolator: a disjunction of conjunctions of literals,
	// "!" prefixing a negated one. Wherever a condition is stored, null means unconditional.
	public sealed class InterpolatorCondition
	{
		// @diogo: past this the #if line stops paying for itself; widening to unconditional is always safe (#443)
		private const int MaxTerms = 8;

		private List<List<string>> m_terms = new List<List<string>>();

		public string Expression { get; private set; }

		private InterpolatorCondition() { }

		public static InterpolatorCondition FromConjunction( List<string> literals )
		{
			InterpolatorCondition result = null;
			if ( literals.Count > 0 )
			{
				List<string> term = new List<string>();
				for ( int i = 0; i < literals.Count; i++ )
				{
					if ( !term.Contains( literals[ i ] ) )
					{
						term.Add( literals[ i ] );
					}
				}
				term.Sort( string.CompareOrdinal );

				result = new InterpolatorCondition();
				result.m_terms.Add( term );
				result.BuildExpression();
			}
			return result;
		}

		public static InterpolatorCondition Or( InterpolatorCondition a, InterpolatorCondition b )
		{
			InterpolatorCondition result = null;
			if ( a != null && b != null )
			{
				List<List<string>> terms = new List<List<string>>( a.m_terms );
				terms.AddRange( b.m_terms );
				if ( Simplify( terms ) && terms.Count <= MaxTerms )
				{
					result = new InterpolatorCondition();
					result.m_terms = terms;
					result.BuildExpression();
				}
			}
			return result;
		}

		public static bool AreEqual( InterpolatorCondition a, InterpolatorCondition b )
		{
			return ( a == null ) ? b == null : ( b != null && a.Expression == b.Expression );
		}

		// @diogo: absorption, plus merging two terms that differ only in one literal's sign. Returns false when the
		// disjunction turned out always true: a merge emptied a term, and that is what null stands for
		private static bool Simplify( List<List<string>> terms )
		{
			bool tautology = false;
			bool changed = true;
			while ( changed && !tautology )
			{
				changed = false;
				for ( int i = 0; i < terms.Count && !changed; i++ )
				{
					for ( int j = 0; j < terms.Count && !changed; j++ )
					{
						if ( i != j )
						{
							if ( terms[ j ].TrueForAll( literal => terms[ i ].Contains( literal ) ) )
							{
								terms.RemoveAt( i );
								changed = true;
							}
							else if ( terms[ i ].Count == terms[ j ].Count )
							{
								List<string> onlyInI = terms[ i ].FindAll( literal => !terms[ j ].Contains( literal ) );
								if ( onlyInI.Count == 1 && terms[ j ].Contains( Negate( onlyInI[ 0 ] ) ) )
								{
									List<string> merged = new List<string>( terms[ i ] );
									merged.Remove( onlyInI[ 0 ] );
									tautology = merged.Count == 0;
									terms[ i ] = merged;
									terms.RemoveAt( j );
									changed = true;
								}
							}
						}
					}
				}
			}
			if ( !tautology )
			{
				terms.Sort( ( x, y ) => string.CompareOrdinal( string.Join( " && ", x ), string.Join( " && ", y ) ) );
			}
			return !tautology;
		}

		private static string Negate( string literal )
		{
			return literal.StartsWith( "!" ) ? literal.Substring( 1 ) : "!" + literal;
		}

		private void BuildExpression()
		{
			List<string> parts = new List<string>();
			for ( int i = 0; i < m_terms.Count; i++ )
			{
				string conjunction = string.Join( " && ", m_terms[ i ] );
				parts.Add( ( m_terms.Count > 1 && m_terms[ i ].Count > 1 ) ? "( " + conjunction + " )" : conjunction );
			}
			Expression = string.Join( " || ", parts );
		}
	}

	[Serializable]
	public class TemplateInterpData
	{
		[SerializeField]
		private string m_interpDataId = string.Empty;

		[SerializeField]
		private int m_interpDataStartIdx = -1;

		[SerializeField]
		private bool m_dynamicMax = false;

		public List<TemplateInterpElement> AvailableInterpolators = new List<TemplateInterpElement>();
		public List<TemplateVertexData> Interpolators = new List<TemplateVertexData>();
		public List<TemplateVertexData> RawInterpolators = new List<TemplateVertexData>();

		public TemplateInterpData() { }

		public bool HasRawInterpolatorOfName( string name )
		{
			return RawInterpolators.Exists( ( x ) => x.VarName.Equals( name ));
		}

		public TemplateInterpData( TemplateInterpData other )
		{
			m_dynamicMax = other.DynamicMax;

			foreach ( TemplateInterpElement data in other.AvailableInterpolators )
			{
				AvailableInterpolators.Add( new TemplateInterpElement( data ) );
			}

			for ( int i = 0; i < other.Interpolators.Count; i++ )
			{
				Interpolators.Add( new TemplateVertexData( other.Interpolators[ i ] ) );
			}

			for( int i = 0; i < other.RawInterpolators.Count; i++ )
			{
				RawInterpolators.Add( new TemplateVertexData( other.RawInterpolators[ i ] ) );
			}
		}


		public void RecalculateAvailableInterpolators( int newMax )
		{
			if( m_dynamicMax )
			{
				if( !TemplateHelperFunctions.IntToSemantic.ContainsKey( ( newMax - 1 ) ) )
				{
					Debug.LogWarning( "Attempting to add inexisting available interpolators" );
					return;
				}

				if( AvailableInterpolators.Count > 0 )
				{
					int currMax = 1 + TemplateHelperFunctions.SemanticToInt[ AvailableInterpolators[ AvailableInterpolators.Count - 1 ].Semantic ];
					if( newMax > currMax )
					{
						int count = newMax - currMax;
						for( int i = 0; i < count; i++ )
						{
							AvailableInterpolators.Add( new TemplateInterpElement( TemplateHelperFunctions.IntToSemantic[ currMax + i ] ));
						}
					}
					else if( newMax < currMax )
					{
						int min = TemplateHelperFunctions.SemanticToInt[ AvailableInterpolators[ 0 ].Semantic ];
						if( newMax > min )
						{
							int count = currMax - newMax;
							for( int i = 0; i < count; i++ )
							{
								AvailableInterpolators.RemoveAt( AvailableInterpolators.Count - 1 );
							}
						}
					}
				}
			}
		}

		public void ReplaceNameOnInterpolator( TemplateSemantics semantic, string newName )
		{
			for ( int i = 0; i < AvailableInterpolators.Count; i++ )
			{
				if ( AvailableInterpolators[ i ].Semantic == semantic )
				{
					AvailableInterpolators[ i ].Name = newName;
					break;
				}
			}
		}

		public void Destroy()
		{
			AvailableInterpolators.Clear();
			AvailableInterpolators = null;

			Interpolators.Clear();
			Interpolators = null;

			RawInterpolators.Clear();
			RawInterpolators = null;
		}
		
		public string InterpDataId { get { return m_interpDataId; } set { m_interpDataId = value; } }
		public int InterpDataStartIdx { get { return m_interpDataStartIdx; } set { m_interpDataStartIdx = value; } }
		public bool DynamicMax { get { return m_dynamicMax; } set { m_dynamicMax = value; } }
	}
}
