// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

//https://www.shadertoy.com/view/XdS3RW
//http://www.deepskycolors.com/archivo/2010/04/21/formulas-for-Photoshop-blending-modes.html
//http://www.pegtop.net/delphi/articles/blendmodes/softlight.htm

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	public enum BlendOps
	{
		ColorBurn,
		ColorDodge,
		Darken,
		Divide,
		Difference,
		Exclusion,
		SoftLight,
		HardLight,
		HardMix,
		Lighten,
		LinearBurn,
		LinearDodge,
		LinearLight,
		Multiply,
		Overlay,
		PinLight,
		Subtract,
		Screen,
		VividLight,
		DarkerColor,
		LighterColor,
		Hue,
		Saturation,
		Color,
		Luminosity
	}
	[Serializable]
	[NodeAttributes( "Blend Operations", "Image Effects", "Common layer blending modes" )]
	public class BlendOpsNode : ParentNode
	{
		private const string BlendOpsModeStr = "Blend Op";
		private const string SaturateResultStr = "Saturate";

		[SerializeField]
		private BlendOps m_currentBlendOp = BlendOps.ColorBurn;

		[SerializeField]
		private WirePortDataType m_mainDataType = WirePortDataType.COLOR;

		[SerializeField]
		private bool m_saturate = true;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.COLOR, false, "Blend" );
			AddInputPort( WirePortDataType.COLOR, false, "Base" );
			AddInputPort( WirePortDataType.FLOAT, false,"Alpha" );
			m_inputPorts[ 2 ].FloatInternalData = 1;
			AddOutputPort( WirePortDataType.COLOR, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].AddPortForbiddenTypes(	WirePortDataType.FLOAT2x2,
														WirePortDataType.FLOAT3x3,
														WirePortDataType.FLOAT4x4,
														WirePortDataType.SAMPLER1D,
														WirePortDataType.SAMPLER2D,
														WirePortDataType.SAMPLER3D,
														WirePortDataType.SAMPLERCUBE,
														WirePortDataType.SAMPLER2DARRAY );
			m_inputPorts[ 1 ].AddPortForbiddenTypes(	WirePortDataType.FLOAT2x2,
														WirePortDataType.FLOAT3x3,
														WirePortDataType.FLOAT4x4,
														WirePortDataType.SAMPLER1D,
														WirePortDataType.SAMPLER2D,
														WirePortDataType.SAMPLER3D,
														WirePortDataType.SAMPLERCUBE,
														WirePortDataType.SAMPLER2DARRAY );

			// @diogo: Alpha feeds the blend lerp, which a texture object cannot be assigned into
			m_inputPorts[ 2 ].AddPortForbiddenTypes( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D,
				WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY, WirePortDataType.SAMPLERSTATE );

			m_textLabelWidth = 75;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_currentBlendOp ) );
			m_useInternalPortData = true;
			m_previewShaderGUID = "6d6b3518705b3ba49acdc6e18e480257";
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			m_previewMaterialPassId = (int)m_currentBlendOp;
			PreviewMaterial.SetInt( "_Sat", m_saturate ? 1 : 0 );
			int lerpMode = ( m_inputPorts[ 2 ].IsConnected || m_inputPorts[ 2 ].FloatInternalData < 1 ) ? 1 : 0;
			PreviewMaterial.SetInt( "_Lerp", lerpMode );
		}

		public override void AfterCommonInit()
		{
			base.AfterCommonInit();

			if( PaddingTitleLeft == 0 )
			{
				PaddingTitleLeft = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
				if( PaddingTitleRight == 0 )
					PaddingTitleRight = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
			}
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnection( portId );
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnection( inputPortId );
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateDisconnection( portId );
		}

		void UpdateConnection( int portId )
		{
			if( portId == 2 )
				return;

			m_inputPorts[ portId ].MatchPortToConnection();
			int otherPortId = ( portId + 1 ) % 2;
			if( m_inputPorts[ otherPortId ].IsConnected )
			{
				m_mainDataType = UIUtils.GetPriority( m_inputPorts[ 0 ].DataType ) > UIUtils.GetPriority( m_inputPorts[ 1 ].DataType ) ? m_inputPorts[ 0 ].DataType : m_inputPorts[ 1 ].DataType;
			}
			else
			{
				m_mainDataType = m_inputPorts[ portId ].DataType;
				m_inputPorts[ otherPortId ].ChangeType( m_mainDataType, false );
			}
			m_outputPorts[ 0 ].ChangeType( m_mainDataType, false );
		}

		void UpdateDisconnection( int portId )
		{
			if( portId == 2 )
				return;

			int otherPortId = ( portId + 1 ) % 2;
			if( m_inputPorts[ otherPortId ].IsConnected )
			{
				m_mainDataType = m_inputPorts[ otherPortId ].DataType;
				m_inputPorts[ portId ].ChangeType( m_mainDataType, false );
				m_outputPorts[ 0 ].ChangeType( m_mainDataType, false );
			}
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_currentBlendOp = (BlendOps)EditorGUILayoutEnumPopup( BlendOpsModeStr, m_currentBlendOp );
			if( EditorGUI.EndChangeCheck() )
			{
				SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_currentBlendOp ) );
			}
			m_saturate = EditorGUILayoutToggle( SaturateResultStr, m_saturate );
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			m_upperLeftWidget.DrawWidget<BlendOps>( ref m_currentBlendOp, this, OnWidgetUpdate );
		}

		private readonly Action<ParentNode> OnWidgetUpdate = ( x ) =>
		{
			x.SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, ( x as BlendOpsNode ).m_currentBlendOp ) );
		};

		// The color blend modes (DarkerColor/LighterColor/Hue/Saturation/Color/Luminosity)
		// operate on the RGB triplet regardless of the node's data type width.
		private string ToRGB( string localVar )
		{
			switch ( m_mainDataType )
			{
				case WirePortDataType.FLOAT:
				{
					return localVar + ".xxx";
				}
				case WirePortDataType.FLOAT2:
				{
					return "float3( " + localVar + ", 0.0 )";
				}
				default:
				{
					return localVar + ".xyz";
				}
			}
		}

		// Fits a float3 RGB result back to the node's output width, preserving the base alpha for 4-channel outputs.
		private string WrapRGB( string rgbExpr, string dstLocalVar )
		{
			switch ( m_outputPorts[ 0 ].DataType )
			{
				case WirePortDataType.FLOAT:
				{
					return "( " + rgbExpr + " ).x";
				}
				case WirePortDataType.FLOAT2:
				{
					return "( " + rgbExpr + " ).xy";
				}
				case WirePortDataType.FLOAT3:
				{
					return rgbExpr;
				}
				default:
				{
					return "float4( " + rgbExpr + ", ( " + dstLocalVar + " ).w )";
				}
			}
		}

		// Registers the luminance-preserving helpers used by the Photoshop HSL color blend modes (Adobe blend spec).
		private void AddBlendColorFunctions( ref MasterNodeDataCollector dataCollector )
		{
			if ( dataCollector.HasFunction( "ASEBlendLum" ) )
			{
				return;
			}

			int currIndent = UIUtils.ShaderIndentLevel;
			if ( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				UIUtils.ShaderIndentLevel = 0;
			}
			else
			{
				UIUtils.ShaderIndentLevel = 1;
				UIUtils.ShaderIndentLevel++;
			}

			string tabs = UIUtils.ShaderIndentTabs;

			dataCollector.AddFunction( "ASEBlendLum",
				tabs + "float ASEBlendLum( float3 c )\n" +
				tabs + "{\n" +
				tabs + "\treturn dot( c, float3( 0.3, 0.59, 0.11 ) );\n" +
				tabs + "}" );

			dataCollector.AddFunction( "ASEBlendClipColor",
				tabs + "float3 ASEBlendClipColor( float3 c )\n" +
				tabs + "{\n" +
				tabs + "\tfloat l = ASEBlendLum( c );\n" +
				tabs + "\tfloat n = min( min( c.r, c.g ), c.b );\n" +
				tabs + "\tfloat x = max( max( c.r, c.g ), c.b );\n" +
				tabs + "\tif ( n < 0.0 ) c = l + ( ( c - l ) * l ) / ( l - n );\n" +
				tabs + "\tif ( x > 1.0 ) c = l + ( ( c - l ) * ( 1.0 - l ) ) / ( x - l );\n" +
				tabs + "\treturn c;\n" +
				tabs + "}" );

			dataCollector.AddFunction( "ASEBlendSetLum",
				tabs + "float3 ASEBlendSetLum( float3 c, float l )\n" +
				tabs + "{\n" +
				tabs + "\tc += l - ASEBlendLum( c );\n" +
				tabs + "\treturn ASEBlendClipColor( c );\n" +
				tabs + "}" );

			dataCollector.AddFunction( "ASEBlendSat",
				tabs + "float ASEBlendSat( float3 c )\n" +
				tabs + "{\n" +
				tabs + "\treturn max( max( c.r, c.g ), c.b ) - min( min( c.r, c.g ), c.b );\n" +
				tabs + "}" );

			dataCollector.AddFunction( "ASEBlendSetSat",
				tabs + "float3 ASEBlendSetSat( float3 c, float s )\n" +
				tabs + "{\n" +
				tabs + "\tfloat mn = min( min( c.r, c.g ), c.b );\n" +
				tabs + "\tfloat mx = max( max( c.r, c.g ), c.b );\n" +
				tabs + "\treturn ( mx > mn ) ? ( ( c - mn ) * s / ( mx - mn ) ) : float3( 0.0, 0.0, 0.0 );\n" +
				tabs + "}" );

			UIUtils.ShaderIndentLevel = currIndent;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string src = m_inputPorts[ 0 ].GenerateShaderForOutput( ref dataCollector, m_mainDataType, false, true );
			string dst = m_inputPorts[ 1 ].GenerateShaderForOutput( ref dataCollector, m_mainDataType, false, true );

			string srcLocalVar = "blendOpSrc" + OutputId;
			string dstLocalVar = "blendOpDest" + OutputId;
			dataCollector.AddLocalVariable( UniqueId, UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_mainDataType ) + " " + srcLocalVar, src + ";" );
			dataCollector.AddLocalVariable( UniqueId, UIUtils.PrecisionWirePortToCgType( CurrentPrecisionType, m_mainDataType ) + " " + dstLocalVar, dst + ";" );

			int currIndent = UIUtils.ShaderIndentLevel;
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				UIUtils.ShaderIndentLevel = 0;
			}
			else
			{
				UIUtils.ShaderIndentLevel = 1;
				UIUtils.ShaderIndentLevel++;
			}

			string result = string.Empty;
			switch( m_currentBlendOp )
			{
				case BlendOps.ColorBurn:
				{
					result = string.Format( "( 1.0 - ( ( 1.0 - {0}) / max( {1}, 0.00001) ) )", dstLocalVar, srcLocalVar);
				}
				break;
				case BlendOps.ColorDodge:
				{
					result = string.Format(  "( {0}/ max( 1.0 - {1}, 0.00001 ) )", dstLocalVar, srcLocalVar );
				}
				break;
				case BlendOps.Darken:
				{
					result = "min( " + srcLocalVar + " , " + dstLocalVar + " )";
				}
				break;
				case BlendOps.Divide:
				{
					result = string.Format( "( {0} / max({1},0.00001) )", dstLocalVar, srcLocalVar );
				}
				break;
				case BlendOps.Difference:
				{
					result = "abs( " + srcLocalVar + " - " + dstLocalVar + " )";
				}
				break;
				case BlendOps.Exclusion:
				{
					result = "( 0.5 - 2.0 * ( " + srcLocalVar + " - 0.5 ) * ( " + dstLocalVar + " - 0.5 ) )";
				}
				break;
				case BlendOps.SoftLight:
				{
					result = string.Format( "(( {1} > 0.5 ) ? ( sqrt( {0} ) * ( 2.0 * {1} - 1.0 ) + 2.0 * {0} * ( 1.0 - {1} ) ) : ( 2.0 * {0} * {1} + {0} * {0} * ( 1.0 - 2.0 * {1} ) ) )", dstLocalVar, srcLocalVar );
				}
				break;
				case BlendOps.HardLight:
				{
					result = " (( " + srcLocalVar + " > 0.5 ) ? ( 1.0 - ( 1.0 - 2.0 * ( " + srcLocalVar + " - 0.5 ) ) * ( 1.0 - " + dstLocalVar + " ) ) : ( 2.0 * " + srcLocalVar + " * " + dstLocalVar + " ) )";
				}
				break;
				case BlendOps.HardMix:
				{
					result = " round( 0.5 * ( " + srcLocalVar + " + " + dstLocalVar + " ) )";
				}
				break;
				case BlendOps.Lighten:
				{
					result = "	max( " + srcLocalVar + ", " + dstLocalVar + " )";
				}
				break;
				case BlendOps.LinearBurn:
				{
					result = "( " + srcLocalVar + " + " + dstLocalVar + " - 1.0 )";
				}
				break;
				case BlendOps.LinearDodge:
				{
					result = "( " + srcLocalVar + " + " + dstLocalVar + " )";
				}
				break;
				case BlendOps.LinearLight:
				{
					result = "(( " + srcLocalVar + " > 0.5 )? ( " + dstLocalVar + " + 2.0 * " + srcLocalVar + " - 1.0 ) : ( " + dstLocalVar + " + 2.0 * ( " + srcLocalVar + " - 0.5 ) ) )";
				}
				break;
				case BlendOps.Multiply:
				{
					result = "( " + srcLocalVar + " * " + dstLocalVar + " )";
				}
				break;
				case BlendOps.Overlay:
				{
					result = "(( " + dstLocalVar + " > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - " + dstLocalVar + " ) * ( 1.0 - " + srcLocalVar + " ) ) : ( 2.0 * " + dstLocalVar + " * " + srcLocalVar + " ) )";
				}
				break;
				case BlendOps.PinLight:
				{
					result = "(( " + srcLocalVar + " > 0.5 ) ? max( " + dstLocalVar + ", 2.0 * ( " + srcLocalVar + " - 0.5 ) ) : min( " + dstLocalVar + ", 2.0 * " + srcLocalVar + " ) )";
				}
				break;
				case BlendOps.Subtract:
				{
					result = "( " + dstLocalVar + " - " + srcLocalVar + " )";
				}
				break;
				case BlendOps.Screen:
				{
					result = "( 1.0 - ( 1.0 - " + srcLocalVar + " ) * ( 1.0 - " + dstLocalVar + " ) )";
				}
				break;
				case BlendOps.VividLight:
				{
					result = string.Format( "(( {0} > 0.5 ) ? ( {1} / max( ( 1.0 - {0} ) * 2.0 ,0.00001) ) : ( 1.0 - ( ( ( 1.0 - {1} ) * 0.5 ) / max( {0},0.00001) ) ) )", srcLocalVar, dstLocalVar);
				}
				break;
				case BlendOps.DarkerColor:
				{
					result = "( ( dot( " + ToRGB( srcLocalVar ) + ", float3( 0.3, 0.59, 0.11 ) ) < dot( " + ToRGB( dstLocalVar ) + ", float3( 0.3, 0.59, 0.11 ) ) ) ? " + srcLocalVar + " : " + dstLocalVar + " )";
				}
				break;
				case BlendOps.LighterColor:
				{
					result = "( ( dot( " + ToRGB( srcLocalVar ) + ", float3( 0.3, 0.59, 0.11 ) ) > dot( " + ToRGB( dstLocalVar ) + ", float3( 0.3, 0.59, 0.11 ) ) ) ? " + srcLocalVar + " : " + dstLocalVar + " )";
				}
				break;
				case BlendOps.Hue:
				{
					AddBlendColorFunctions( ref dataCollector );
					result = WrapRGB( "ASEBlendSetLum( ASEBlendSetSat( " + ToRGB( srcLocalVar ) + ", ASEBlendSat( " + ToRGB( dstLocalVar ) + " ) ), ASEBlendLum( " + ToRGB( dstLocalVar ) + " ) )", dstLocalVar );
				}
				break;
				case BlendOps.Saturation:
				{
					AddBlendColorFunctions( ref dataCollector );
					result = WrapRGB( "ASEBlendSetLum( ASEBlendSetSat( " + ToRGB( dstLocalVar ) + ", ASEBlendSat( " + ToRGB( srcLocalVar ) + " ) ), ASEBlendLum( " + ToRGB( dstLocalVar ) + " ) )", dstLocalVar );
				}
				break;
				case BlendOps.Color:
				{
					AddBlendColorFunctions( ref dataCollector );
					result = WrapRGB( "ASEBlendSetLum( " + ToRGB( srcLocalVar ) + ", ASEBlendLum( " + ToRGB( dstLocalVar ) + " ) )", dstLocalVar );
				}
				break;
				case BlendOps.Luminosity:
				{
					AddBlendColorFunctions( ref dataCollector );
					result = WrapRGB( "ASEBlendSetLum( " + ToRGB( dstLocalVar ) + ", ASEBlendLum( " + ToRGB( srcLocalVar ) + " ) )", dstLocalVar );
				}
				break;
			}

			UIUtils.ShaderIndentLevel = currIndent;
			if( m_inputPorts[ 2 ].IsConnected || m_inputPorts[ 2 ].FloatInternalData < 1.0 )
			{
				string opacity = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
				string lerpVar = "lerpBlendMode" + OutputId;
				string lerpResult = string.Format( "lerp({0},{1},{2})", dstLocalVar, result, opacity );
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, m_outputPorts[ 0 ].DataType, lerpVar, lerpResult );
				result = lerpVar;				
			}

			if( m_saturate )
				result = "( saturate( " + result + " ))";

			return CreateOutputLocalVariable( 0, result, ref dataCollector );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentBlendOp );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_saturate );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_currentBlendOp = (BlendOps)Enum.Parse( typeof( BlendOps ), GetCurrentParam( ref nodeParams ) );
			m_saturate = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_currentBlendOp ) );
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}
	}
}
