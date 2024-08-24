// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Transform Position", "Object Transform", "Transforms a position value from one space to another" )]
	public sealed class TransformPositionNode : ParentNode
	{
		[SerializeField]
		private TransformSpaceFrom m_from = TransformSpaceFrom.Object;

		[SerializeField]
		private TransformSpaceTo m_to = TransformSpaceTo.World;

		[SerializeField]
		private bool m_perspectiveDivide = false;

		[SerializeField]
		private InverseTangentType m_inverseTangentType = InverseTangentType.Fast;

		[SerializeField]
		private bool m_absoluteWorldPos = true;

		private string InverseTBNStr = "Inverse TBN";

		private const string AseObjectToWorldPosVarName = "objToWorld";
		private const string AseObjectToWorldPosFormat = "mul( unity_ObjectToWorld, float4( {0}, 1 ) ).xyz";
		private const string AseHDObjectToWorldPosFormat = "mul( GetObjectToWorldMatrix(), float4( {0}, 1 ) ).xyz";
		private const string ASEHDAbsoluteWordPos = "GetAbsolutePositionWS({0})";
		private const string ASEHDRelaviveCameraPos = "GetCameraRelativePositionWS({0})";
		private const string AseObjectToViewPosVarName = "objToView";
		private const string AseObjectToViewPosFormat = "mul( UNITY_MATRIX_MV, float4( {0}, 1 ) ).xyz";
		private const string AseHDObjectToViewPosFormat = "TransformWorldToView( TransformObjectToWorld({0}) )";

		private const string AseWorldToObjectPosVarName = "worldToObj";
		private const string AseWorldToObjectPosFormat = "mul( unity_WorldToObject, float4( {0}, 1 ) ).xyz";
		private const string AseSRPWorldToObjectPosFormat = "mul( GetWorldToObjectMatrix(), float4( {0}, 1 ) ).xyz";


		private const string AseWorldToViewPosVarName = "worldToView";
		private const string AseWorldToViewPosFormat = "mul( UNITY_MATRIX_V, float4( {0}, 1 ) ).xyz";

		private const string AseViewToObjectPosVarName = "viewToObj";
		private const string AseViewToObjectPosFormat = "mul( unity_WorldToObject, mul( UNITY_MATRIX_I_V , float4( {0}, 1 ) ) ).xyz";
		private const string AseHDViewToObjectPosFormat = "mul( GetWorldToObjectMatrix(), mul( UNITY_MATRIX_I_V , float4( {0}, 1 ) ) ).xyz";

		private const string AseViewToWorldPosVarName = "viewToWorld";
		private const string AseViewToWorldPosFormat = "mul( UNITY_MATRIX_I_V, float4( {0}, 1 ) ).xyz";

		///////////////////////////////////////////////////////////
		// ToClipPos
		private const string AseObjectToClipPosVarName = "objectToClip";
		private const string AseObjectToClipPosFormat = "UnityObjectToClipPos({0})";
		private const string AseSRPObjectToClipPosFormat = "TransformObjectToHClip({0})";

		private const string AseWorldToClipPosVarName = "worldToClip";
		private const string AseWorldToClipPosFormat = "mul(UNITY_MATRIX_VP, float4({0}, 1.0))";
		private const string AseSRPWorldToClipPosFormat = "TransformWorldToHClip({0})";

		private const string AseViewToClipPosVarName = "viewToClip";
		private const string AseViewToClipPosFormat = "mul(UNITY_MATRIX_P, float4({0}, 1.0))";
		private const string AseSRPViewToClipPosFormat = "TransformWViewToHClip({0})";

		///////////////////////////////////////////////////////////
		// ToScreenPos
		private const string AseObjectToScreenVarName = "objectToScreen";
		private const string AseWorldToScreenVarName = "worldToScreen";
		private const string AseViewToScreenVarName = "viewToScreen";

		private const string AseToScreenFormat = "ComputeScreenPos( {0} )";
		private const string AseHDRPToScreenFormat = "ComputeScreenPos( {0} _ProjectionParams.x )";

		//
		private const string AseClipToNDC = "{0}/{0}.w";
		/////////////////////////////////////////////////////
		private const string AseObjectToTangentPosVarName = "objectToTangentPos";
		private const string AseWorldToTangentPosVarName = "worldToTangentPos";
		private const string AseViewToTangentPosVarName = "viewToTangentPos";
		private const string ASEWorldToTangentFormat = "mul( ase_worldToTangent, {0})";


		private const string AseTangentToObjectVarName = "tangentToObject";
		private const string AseTangentToWorldVarName = "tangentToWorld";
		private const string AseTangentToViewVarName = "tangentToView";
		private const string AseTangentToClipVarName = "tangentToClip";
		private const string AseTangentToScreenVarName = "tangentToScreen";
		private const string ASEMulOpFormat = "mul( {0}, {1} )";


		///////////////////////////////////////////////////////////
		private const string FromStr = "From";
		private const string ToStr = "To";
		private const string PerpectiveDivideStr = "Perspective Divide";
		private const string SubtitleFormat = "{0} to {1}";

		private readonly string[] m_spaceOptionsFrom =
		{
			"Object",
			"World",
			"View",
			"Tangent"
		};

		private readonly string[] m_spaceOptionsTo =
		{
			"Object",
			"World",
			"View",
			"Tangent",
			"Clip",
			"Screen"
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, Constants.EmptyPortValue );
			AddOutputVectorPorts( WirePortDataType.FLOAT4, "XYZ" );
			m_outputPorts[ 4 ].Visible = false;
			m_useInternalPortData = true;
			m_autoWrapProperties = true;
			m_previewShaderGUID = "74e4d859fbdb2c0468de3612145f4929";
			m_textLabelWidth = 120;
			UpdateSubtitle();
			UpdateOutputPort();
		}

		private void UpdateSubtitle()
		{
			SetAdditonalTitleText( string.Format( SubtitleFormat, m_from, m_to ) );
		}
		public override void OnOutputPortConnected( int portId, int otherNodeId, int otherPortId )
		{
			base.OnOutputPortConnected( portId, otherNodeId, otherPortId );
			UpdateOutputPort();
		}

		void UpdateOutputPort()
		{
			switch ( m_to )
			{
				case TransformSpaceTo.Clip:
				{
					m_outputPorts[ 0 ].ChangeProperties( "XYZW", WirePortDataType.FLOAT4, false );
					var wire = m_outputPorts[ 0 ].GetConnection();
					if ( wire != null && wire.DataType != WirePortDataType.FLOAT4 )
					{
						wire.DataType = WirePortDataType.FLOAT4;
					}
					m_outputPorts[ 4 ].Visible = true;
					break;
				}
				default:
				{
					m_outputPorts[ 0 ].ChangeProperties( "XYZ", WirePortDataType.FLOAT3, false );
					m_outputPorts[ 4 ].Visible = false;
					break;
				}
			}
			m_sizeIsDirty = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_from = (TransformSpaceFrom)EditorGUILayoutPopup( FromStr, (int)m_from, m_spaceOptionsFrom );
			m_to = (TransformSpaceTo)EditorGUILayoutPopup( ToStr, (int)m_to, m_spaceOptionsTo );
			if( m_from == TransformSpaceFrom.Tangent )
			{
				m_inverseTangentType = (InverseTangentType)EditorGUILayoutEnumPopup( InverseTBNStr, m_inverseTangentType );
			}
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateSubtitle();
				UpdateOutputPort();
			}

			if( m_to == TransformSpaceTo.Clip )
			{
				m_perspectiveDivide = EditorGUILayoutToggle( PerpectiveDivideStr, m_perspectiveDivide );
			}
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData, ref dataCollector );
			if( (int)m_from != (int)m_to && ( m_from == TransformSpaceFrom.Tangent || m_to == TransformSpaceTo.Tangent ) )
				dataCollector.DirtyNormal = true;
		}

		void CalculateTransform( TransformSpaceFrom from, TransformSpaceTo to, ref MasterNodeDataCollector dataCollector, ref string varName, ref string result )
		{
			switch( from )
			{
				case TransformSpaceFrom.Object:
				{
					switch( to )
					{
						default:
						case TransformSpaceTo.Object:
						{
							// no transform
							break;
						}
						case TransformSpaceTo.World:
						{
							if( dataCollector.IsTemplate && dataCollector.IsSRP )
							{
								if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								{
									result = string.Format( AseHDObjectToWorldPosFormat, result );
									if( m_absoluteWorldPos )
									{
										result = string.Format( ASEHDAbsoluteWordPos, result );
									}
								}
								else if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.URP )
								{
									result = string.Format( AseHDObjectToWorldPosFormat, result );
								}
							}
							else
							{
								result = string.Format( AseObjectToWorldPosFormat, result );
							}
							varName = AseObjectToWorldPosVarName + OutputId;
							break;
						}
						case TransformSpaceTo.View:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								result = string.Format( AseHDObjectToViewPosFormat, result );
							else
								result = string.Format( AseObjectToViewPosFormat, result );
							varName = AseObjectToViewPosVarName + OutputId;
							break;
						}
						case TransformSpaceTo.Clip:
						case TransformSpaceTo.Screen:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
							{
								result = string.Format( AseSRPObjectToClipPosFormat, result );
							}
							else
							{
								result = string.Format( AseObjectToClipPosFormat, result );
							}

							if ( to == TransformSpaceTo.Screen )
							{
								if ( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								{
									result = string.Format( AseHDRPToScreenFormat, result );
								}
								else
								{
									result = string.Format( AseToScreenFormat, result );
								}
								varName = AseObjectToScreenVarName + OutputId;
							}
							else // TransformSpaceTo.Clip
							{
								varName = AseObjectToClipPosVarName + OutputId;
							}
							break;
						}
					}
					break;
				}
				case TransformSpaceFrom.World:
				{
					switch( to )
					{
						default:
						case TransformSpaceTo.World:
						{
							// no transform
							break;
						}
						case TransformSpaceTo.Object:
						{
							if( dataCollector.IsTemplate && dataCollector.IsSRP )
							{
								if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								{
									if( m_absoluteWorldPos )
									{
										result = string.Format( ASEHDRelaviveCameraPos, result );
									}
									result = string.Format( AseSRPWorldToObjectPosFormat, result );
								}
								else if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.URP )
								{
									result = string.Format( AseSRPWorldToObjectPosFormat, result );
								}

							}
							else
								result = string.Format( AseWorldToObjectPosFormat, result );
							varName = AseWorldToObjectPosVarName + OutputId;
							break;
						}
						case TransformSpaceTo.View:
						{
							result = string.Format( AseWorldToViewPosFormat, result );
							varName = AseWorldToViewPosVarName + OutputId;
							break;
						}
						case TransformSpaceTo.Clip:
						case TransformSpaceTo.Screen:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
							{
								result = string.Format( AseSRPWorldToClipPosFormat, result );
							}
							else
							{
								result = string.Format( AseWorldToClipPosFormat, result );
							}

							if ( to == TransformSpaceTo.Screen )
							{
								if ( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								{
									result = string.Format( AseHDRPToScreenFormat, result );
								}
								else
								{
									result = string.Format( AseToScreenFormat, result );
								}
								varName = AseWorldToScreenVarName + OutputId;
							}
							else // TransformSpaceTo.Clip
							{
								varName = AseWorldToClipPosVarName + OutputId;
							}
							break;
						}
					}
					break;
				}
				case TransformSpaceFrom.View:
				{
					switch( to )
					{
						default:
						case TransformSpaceTo.View:
						{
							// no transform
							break;
						}
						case TransformSpaceTo.Object:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								result = string.Format( AseHDViewToObjectPosFormat, result );
							else
								result = string.Format( AseViewToObjectPosFormat, result );
							varName = AseViewToObjectPosVarName + OutputId;
						}
						break;
						case TransformSpaceTo.World:
						{
							result = string.Format( AseViewToWorldPosFormat, result ); 
							if( dataCollector.IsTemplate && 
								dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP &&
								m_absoluteWorldPos )
							{
								result = string.Format( ASEHDAbsoluteWordPos , result );
							}
							varName = AseViewToWorldPosVarName + OutputId;
							break;
						}
						case TransformSpaceTo.Clip:
						case TransformSpaceTo.Screen:
							{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
							{
								result = string.Format( AseSRPViewToClipPosFormat, result );
							}
							else
							{
								result = string.Format( AseViewToClipPosFormat, result );
							}

							if ( to == TransformSpaceTo.Screen )
							{
								if ( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								{
									result = string.Format( AseHDRPToScreenFormat, result );
								}
								else
								{
									result = string.Format( AseToScreenFormat, result );
								}
								varName = AseViewToScreenVarName + OutputId;
							}
							else // TransformSpaceTo.Clip
							{
								varName = AseViewToClipPosVarName + OutputId;
							}
							break;
						}
					}
					break;
				}
				default:
				{
					break;
				}
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );

			GeneratorUtils.RegisterUnity2019MatrixDefines( ref dataCollector );

			string result = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string varName = string.Empty;

			if( (int)m_from == (int)m_to )
			{
				RegisterLocalVariable( 0, result, ref dataCollector );
				return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );
			}

			switch( m_from )
			{
				case TransformSpaceFrom.Object:
				{
					switch( m_to )
					{
						default:
						case TransformSpaceTo.Object:
						{
							// no transform
							break;
						}
						case TransformSpaceTo.World:
						case TransformSpaceTo.View:
						case TransformSpaceTo.Clip:
						case TransformSpaceTo.Screen:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
							break;
						}
						case TransformSpaceTo.Tangent:
						{
							GeneratorUtils.GenerateWorldToTangentMatrix( ref dataCollector, UniqueId, CurrentPrecisionType );
							CalculateTransform( m_from, TransformSpaceTo.World, ref dataCollector, ref varName, ref result );
							result = string.Format( ASEWorldToTangentFormat, result );
							varName = AseObjectToTangentPosVarName + OutputId;
							break;
						}
					}
				}
				break;
				case TransformSpaceFrom.World:
				{
					switch( m_to )
					{
						default:
						case TransformSpaceTo.World:
						{
							// no transform
							break;
						}
						case TransformSpaceTo.Object:
						case TransformSpaceTo.View:
						case TransformSpaceTo.Clip:
						case TransformSpaceTo.Screen:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
							break;
						}
						case TransformSpaceTo.Tangent:
						{
							GeneratorUtils.GenerateWorldToTangentMatrix( ref dataCollector, UniqueId, CurrentPrecisionType );
							result = string.Format( ASEWorldToTangentFormat, result );
							varName = AseWorldToTangentPosVarName + OutputId;
						}
						break;
					}
					break;
				}
				case TransformSpaceFrom.View:
				{
					switch( m_to )
					{
						default:
						case TransformSpaceTo.View:
						{
							// no transform
							break;
						}
						case TransformSpaceTo.Object:
						case TransformSpaceTo.World:
						case TransformSpaceTo.Clip:
						case TransformSpaceTo.Screen:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
							break;
						}
						case TransformSpaceTo.Tangent:
						{
							GeneratorUtils.GenerateWorldToTangentMatrix( ref dataCollector, UniqueId, CurrentPrecisionType );
							CalculateTransform( m_from, TransformSpaceTo.World, ref dataCollector, ref varName, ref result );
							result = string.Format( ASEWorldToTangentFormat, result );
							varName = AseViewToTangentPosVarName + OutputId;
						}
						break;
					}
					break;
				}
				case TransformSpaceFrom.Tangent:
				{
					string matrixVal = string.Empty;
					if( m_inverseTangentType == InverseTangentType.Fast )
						matrixVal = GeneratorUtils.GenerateTangentToWorldMatrixFast( ref dataCollector, UniqueId, CurrentPrecisionType );
					else
						matrixVal = GeneratorUtils.GenerateTangentToWorldMatrixPrecise( ref dataCollector, UniqueId, CurrentPrecisionType );

					switch( m_to )
					{
						default:
						case TransformSpaceTo.Tangent:
						{
							// no transform
							break;
						}
						case TransformSpaceTo.World:
						{
							result = string.Format( ASEMulOpFormat, matrixVal, result );
							varName = AseTangentToWorldVarName + OutputId;
							break;
						}
						case TransformSpaceTo.Object:
						case TransformSpaceTo.View:
						case TransformSpaceTo.Clip:
						case TransformSpaceTo.Screen:
						{
							result = string.Format( ASEMulOpFormat, matrixVal, result );
							CalculateTransform( TransformSpaceFrom.World, m_to, ref dataCollector, ref varName, ref result );

							if ( m_to == TransformSpaceTo.Object )
							{
								varName = AseTangentToObjectVarName + OutputId;
							}
							else if ( m_to == TransformSpaceTo.View )
							{
								varName = AseTangentToViewVarName + OutputId;
							}
							else if ( m_to == TransformSpaceTo.Screen )
							{
								varName = AseTangentToScreenVarName + OutputId;
							}
							else if ( m_to == TransformSpaceTo.Clip )
							{
								varName = AseTangentToClipVarName + OutputId;
							}
							break;
						}
					}
					break;
				}
				default:
				{
					break;
				}
			}

			if ( m_to == TransformSpaceTo.Clip || m_to == TransformSpaceTo.Screen )
			{
				if ( m_perspectiveDivide || m_to == TransformSpaceTo.Screen )
				{
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT4, varName, result );
					result = string.Format( AseClipToNDC, varName );
					varName += "NDC";
				}
				else if ( m_to != TransformSpaceTo.Clip )
				{
					result += ".xyz";
				}
			}

			RegisterLocalVariable( 0, result, ref dataCollector, varName );
			return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			string from = GetCurrentParam( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() < 17500 && from.Equals( "Clip" ) )
			{
				UIUtils.ShowMessage( UniqueId, "Clip Space no longer supported on From field over Transform Position node" );
			}
			else
			{
				m_from = (TransformSpaceFrom)Enum.Parse( typeof( TransformSpaceFrom ), from );
			}
			m_to = (TransformSpaceTo)Enum.Parse( typeof( TransformSpaceTo ), GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 15701 )
			{
				m_perspectiveDivide = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			if( UIUtils.CurrentShaderVersion() > 15800 )
			{
				m_inverseTangentType = (InverseTangentType)Enum.Parse( typeof( InverseTangentType ), GetCurrentParam( ref nodeParams ) );
			}
			if( UIUtils.CurrentShaderVersion() > 16103 )
			{
				m_absoluteWorldPos = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			UpdateSubtitle();
			UpdateOutputPort();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_from );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_to );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_perspectiveDivide );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_inverseTangentType );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_absoluteWorldPos );
		}
	}
}
