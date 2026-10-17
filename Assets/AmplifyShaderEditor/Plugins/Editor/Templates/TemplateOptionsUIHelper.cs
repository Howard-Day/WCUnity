// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateOptionsUIHelper
	{
		public struct ReadOptions
		{
			public string Name;
			public string Selection;
			public Int64 Timestamp;
		}
		private const string CustomOptionsLabel = " Custom Options";

		private bool m_isSubShader = false;

		[SerializeField]
		private bool m_passCustomOptionsFoldout = true;

		[SerializeField]
		private string m_passCustomOptionsLabel = CustomOptionsLabel;

		[SerializeField]
		private int m_passCustomOptionsSizeCheck = 0;

		[SerializeField]
		private List<TemplateOptionUIItem> m_passCustomOptionsUI = new List<TemplateOptionUIItem>();

		[NonSerialized]
		private Dictionary<string, TemplateOptionUIItem> m_passCustomOptionsUIDict = new Dictionary<string, TemplateOptionUIItem>();

		[NonSerialized]
		private TemplateMultiPassMasterNode m_owner;

		[NonSerialized]
		private List<ReadOptions> m_readOptions = null;

		[SerializeField]
		private List<TemplateOptionPortItem> m_passCustomOptionsPorts = new List<TemplateOptionPortItem>();

		public TemplateOptionsUIHelper( bool isSubShader )
		{
			m_isSubShader = isSubShader;
		}

		public void CopyOptionsValuesFrom( TemplateOptionsUIHelper origin )
		{
			for( int i = 0; i < origin.PassCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].CopyValuesFrom( origin.PassCustomOptionsUI[ i ] );
			}
		}

		public void Destroy()
		{
			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].Destroy();
			}

			m_passCustomOptionsUI.Clear();
			m_passCustomOptionsUI = null;

			m_passCustomOptionsUIDict.Clear();
			m_passCustomOptionsUIDict = null;

			m_passCustomOptionsPorts.Clear();
			m_passCustomOptionsPorts = null;
		}

		public void DrawCustomOptions( TemplateMultiPassMasterNode owner )
		{
			m_owner = owner;

			if( m_passCustomOptionsUI.Count > 0 )
			{
				NodeUtils.DrawNestedPropertyGroup( ref m_passCustomOptionsFoldout, m_passCustomOptionsLabel, DrawCustomOptionsBlock );
			}
		}

		public void DrawCustomOptionsBlock()
		{
			float currWidth = EditorGUIUtility.labelWidth;
			float size = Mathf.Max( UIUtils.CurrentWindow.ParametersWindow.TransformedArea.width * 0.385f, 0 );
			EditorGUIUtility.labelWidth = size;
			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].Draw( m_owner );
			}
			EditorGUILayout.Space();
			EditorGUIUtility.labelWidth = currWidth;
		}

		private InputPort MatchInputPortForTemplateAction( TemplateMultiPassMasterNode passMasterNode, TemplateActionItem action )
		{
			InputPort port;
			if ( action.ActionDataIdx > -1 )
			{
				port = passMasterNode.GetInputPortByUniqueId( action.ActionDataIdx );
			}
			else
			{
				// @diogo: hacky.. but it'll have to do, for crude backwards compatibility
				if ( action.ActionData.StartsWith( "_" ) )
				{
					port = passMasterNode.GetInputPortByExternalLinkId( action.ActionData );
				}
				else
				{
					port = passMasterNode.GetInputPortByName( action.ActionData );
				}
			}
			return port;
		}

		private bool TestActionItemConditional( TemplateActionItem actionItem, TemplateActionItemConditional conditional )
		{
			bool succeeded = true;
			if ( conditional != null && conditional.IsValid )
			{
				TemplateOptionUIItem referenceItem = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( conditional.Option ) ) );
				if ( referenceItem != null )
				{
					bool equal = conditional.Value.Equals( referenceItem.Options.Options[ referenceItem.CurrentOption ] );
					if ( conditional.Condition == TemplateActionItemConditional.Conditional.Equal )
					{
						succeeded = equal;
					}
					else if ( conditional.Condition == TemplateActionItemConditional.Conditional.NotEqual )
					{
						succeeded = !equal;
					}
				}
			}
			return succeeded;
		}

		// During shader load the per-pass option cascade can run before every pass has an instantiated
		// master node (e.g. a shader saved before new passes were added to the template); those are
		// reconciled by the deferred options refresh. Only warn when the pass is genuinely missing from
		// the template definition, which points to an actual authoring typo.
		private void LogMissingPass( TemplateMultiPassMasterNode owner, TemplateActionItem action )
		{
			if ( owner.CurrentTemplate != null && owner.CurrentTemplate.ContainsPass( action.PassName ) )
			{
				return;
			}
			Debug.LogFormat( "Could not find pass {0} for action {1} '{2}' on template {3}", action.PassName, action.ActionType, action.ActionData, owner.CurrentTemplate.DefaultShaderName );
		}

		public void OnCustomOptionSelected( bool actionFromUser, bool isRefreshing, bool invertAction, TemplateMultiPassMasterNode owner, TemplateOptionUIItem uiItem, int recursionLevel, params TemplateActionItem[] validActions )
		{
			uiItem.CheckOnExecute = false;
			for( int i = 0; i < validActions.Length; i++ )
			{
				// @diogo: test option conditional before continuing
				if ( !TestActionItemConditional( validActions[ i ], validActions[ i ].OptionConditional ) )
				{
					continue;
				}

				// @diogo: test action conditional before continuing
				if ( !TestActionItemConditional( validActions[ i ], validActions[ i ].ActionConditional ) )
				{
					continue;
				}

				AseOptionsActionType actionType = validActions[ i ].ActionType;
				if( invertAction )
				{
					if( !TemplateOptionsToolsHelper.InvertAction( validActions[ i ].ActionType, ref actionType ) )
					{
						continue;
					}
				}

				switch( actionType )
				{
					case AseOptionsActionType.RefreshOption:
					{
						if ( !uiItem.IsVisible || recursionLevel > 0 )
							break;

						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if ( item != null )
						{
							item.Update( recursionLevel + 1, isRefreshing );
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action '{1}' on template {2}", validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.ShowOption:
					{
						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if( item != null )
						{
							if( isRefreshing )
							{
								string optionId = validActions[ i ].PassName + validActions[ i ].ActionData + "Option";
								TemplatesManager.Instance.SetOptionsValue( optionId, true );
							}

							// this prevents options from showing up when loading by checking if they were hidden by another option
							// it works on the assumption that an option that may possible hide this one is checked first
							if( !isRefreshing )
								item.IsVisible = true;
							else if( item.WasVisible )
								item.IsVisible = true;

							if( !invertAction && validActions[ i ].ActionDataIdx > -1 )
								item.CurrentOption = validActions[ i ].ActionDataIdx;

							item.CheckEnDisable( actionFromUser, isRefreshing );
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action '{1}' on template {2}", validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.HideOption:
					{
						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if( item != null )
						{
							bool flag = false;
							if( isRefreshing )
							{
								string optionId = validActions[ i ].PassName + validActions[ i ].ActionData + "Option";
								flag = TemplatesManager.Instance.SetOptionsValue( optionId, false );

								// @diogo: a hidden driver's disable cascade must force its child hidden; the accumulator's
								// "show wins" bias would otherwise keep e.g. _TessValue visible after Tessellation is turned
								// off, because the Type sub-option showed it earlier in the timestamp-ordered replay
								if( !uiItem.IsVisible )
								{
									flag = false;
								}
							}

							item.IsVisible = false || flag;
							if( !invertAction && validActions[ i ].ActionDataIdx > -1 )
								item.CurrentOption = validActions[ i ].ActionDataIdx;

							item.CheckEnDisable( actionFromUser, isRefreshing );
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action '{1}' on template {2}", validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.SetOption:
					{
						if( !uiItem.IsVisible )
							break;

						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if( item != null )
						{
							item.CurrentOption = validActions[ i ].ActionDataIdx;
							item.Update( recursionLevel, isRefreshing );
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action '{1}' on template {2}", validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.HidePort:
					{
						TemplateMultiPassMasterNode passMasterNode = owner;
						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName,owner.LODIndex );
						}

						if( passMasterNode != null )
						{
							InputPort port = MatchInputPortForTemplateAction( passMasterNode, validActions[ i ] );
							if( port != null )
							{
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + port.Name;
									TemplatesManager.Instance.SetOptionsValue( optionId, port.IsConnected );
									port.Visible = port.IsConnected;
								}
								else
								{
									port.Visible = false;
								}
								passMasterNode.SizeIsDirty = true;
							}
							else
							{
								Debug.LogFormat( "Could not find port {0},{1} for action '{2}' on template {3}", validActions[ i ].ActionDataIdx, validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							LogMissingPass( owner, validActions[ i ] );
						}
					}
					break;
					case AseOptionsActionType.ShowPort:
					{
						if( !uiItem.IsVisible )
							break;

						TemplateMultiPassMasterNode passMasterNode = owner;
						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
						}

						if( passMasterNode != null )
						{
							InputPort port = MatchInputPortForTemplateAction( passMasterNode, validActions[ i ] );
							if( port != null )
							{
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + port.Name;
									TemplatesManager.Instance.SetOptionsValue( optionId, true );
								}

								port.Visible = true;
								passMasterNode.SizeIsDirty = true;
							}
							else
							{
								Debug.LogFormat( "Could not find port {0},{1} for action '{2}' on template {3}", validActions[ i ].ActionDataIdx, validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							LogMissingPass( owner, validActions[ i ] );
						}
					}
					break;
					case AseOptionsActionType.SetPortName:
					{
						if( !uiItem.IsVisible )
							break;

						TemplateMultiPassMasterNode passMasterNode = owner;
						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
						}

						if( passMasterNode != null )
						{
							InputPort port = MatchInputPortForTemplateAction( passMasterNode, validActions[ i ] );
							if( port != null )
							{
								port.Name = validActions[ i ].ActionData2;
								passMasterNode.SizeIsDirty = true;
							}
							else
							{
								Debug.LogFormat( "Could not find port {0},{1} for action '{2}' on template {3}", validActions[ i ].ActionData, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							LogMissingPass( owner, validActions[ i ] );
						}
					}
					break;
					case AseOptionsActionType.SetDefine:
					{
						if( !uiItem.IsVisible )
						{
							uiItem.CheckOnExecute = true;
							break;
						}

						//Debug.Log( "DEFINE " + validActions[ i ].ActionData );
						if( validActions[ i ].AllPasses )
						{
							string actionData = validActions[ i ].ActionData;
							string defineValue = string.Empty;
							bool isPragma = false;
							if( actionData.StartsWith( "pragma" ) )
							{
								defineValue = "#" + actionData;
								isPragma = true;
							}
							else
							{
								defineValue = "#define " + validActions[ i ].ActionData;
							}
							if( isRefreshing )
							{
								TemplatesManager.Instance.SetOptionsValue( defineValue, true );
							}
							List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes( owner.LODIndex );
							int count = nodes.Count;
							for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
							{
								nodes[ nodeIdx ].OptionsDefineContainer.AddDirective( defineValue, false, isPragma );
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								string actionData = validActions[ i ].ActionData;
								string defineValue = string.Empty;
								bool isPragma = false;
								if( actionData.StartsWith( "pragma" ) )
								{
									defineValue = "#" + actionData;
									isPragma = true;
								}
								else
								{
									defineValue = "#define " + validActions[ i ].ActionData;
								}
								if( isRefreshing )
								{
									string optionsId = validActions[ i ].PassName + defineValue;
									TemplatesManager.Instance.SetOptionsValue( optionsId, true );
								}
								passMasterNode.OptionsDefineContainer.AddDirective( defineValue, false, isPragma );
							}
							else
							{
								LogMissingPass( owner, validActions[ i ] );
							}
						}
						else
						{
							uiItem.CheckOnExecute = true;
						}
					}
					break;
					case AseOptionsActionType.RemoveDefine:
					{
						//Debug.Log( "UNDEFINE " + validActions[ i ].ActionData );
						if( validActions[ i ].AllPasses )
						{
							string actionData = validActions[ i ].ActionData;
							string defineValue = string.Empty;
							if( actionData.StartsWith( "pragma" ) )
							{
								defineValue = "#" + actionData;
							}
							else
							{
								defineValue = "#define " + validActions[ i ].ActionData;
							}

							bool flag = false;
							if( isRefreshing )
							{
								flag = TemplatesManager.Instance.SetOptionsValue( defineValue, false );
							}

							if( !flag )
							{
								List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes( owner.LODIndex );
								int count = nodes.Count;
								for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
								{
									nodes[ nodeIdx ].OptionsDefineContainer.RemoveDirective( defineValue );
								}
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								string actionData = validActions[ i ].ActionData;
								string defineValue = string.Empty;
								if( actionData.StartsWith( "pragma" ) )
								{
									defineValue = "#" + actionData;
								}
								else
								{
									defineValue = "#define " + validActions[ i ].ActionData;
								}
								bool flag = false;
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + defineValue;
									flag = TemplatesManager.Instance.SetOptionsValue( optionId, false );
								}
								if( !flag )
								{
									passMasterNode.OptionsDefineContainer.RemoveDirective( defineValue );
								}
							}
							else
							{
								LogMissingPass( owner, validActions[ i ] );
							}
						}
						else
						{
							uiItem.CheckOnExecute = false;
						}
					}
					break;
					case AseOptionsActionType.SetUndefine:
					{
						if( !uiItem.IsVisible )
						{
							uiItem.CheckOnExecute = true;
							break;
						}

						if( validActions[ i ].AllPasses )
						{
							string defineValue = "#undef " + validActions[ i ].ActionData;
							if( isRefreshing )
							{
								TemplatesManager.Instance.SetOptionsValue( defineValue, true );
							}
							List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes(owner.LODIndex);
							int count = nodes.Count;
							for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
							{
								nodes[ nodeIdx ].OptionsDefineContainer.AddDirective( defineValue, false );
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								string defineValue = "#undef " + validActions[ i ].ActionData;
								if( isRefreshing )
								{
									string optionsId = validActions[ i ].PassName + defineValue;
									TemplatesManager.Instance.SetOptionsValue( optionsId, true );
								}
								passMasterNode.OptionsDefineContainer.AddDirective( defineValue, false );
							}
							else
							{
								LogMissingPass( owner, validActions[ i ] );
							}
						}
						else
						{
							uiItem.CheckOnExecute = true;
						}
					}
					break;
					case AseOptionsActionType.RemoveUndefine:
					{
						if( validActions[ i ].AllPasses )
						{
							string defineValue = "#undef " + validActions[ i ].ActionData;
							bool flag = false;
							if( isRefreshing )
							{
								flag = TemplatesManager.Instance.SetOptionsValue( defineValue, false );
							}

							if( !flag )
							{
								List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes( owner.LODIndex );
								int count = nodes.Count;
								for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
								{
									nodes[ nodeIdx ].OptionsDefineContainer.RemoveDirective( defineValue );
								}
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								bool flag = false;
								string defineValue = "#undef " + validActions[ i ].ActionData;
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + defineValue;
									flag = TemplatesManager.Instance.SetOptionsValue( optionId, false );
								}

								if( !flag )
								{
									passMasterNode.OptionsDefineContainer.RemoveDirective( defineValue );
								}
							}
							else
							{
								LogMissingPass( owner, validActions[ i ] );
							}
						}
						else
						{
							uiItem.CheckOnExecute = false;
						}
					}
					break;
					case AseOptionsActionType.ExcludePass:
					{
						string optionId = validActions[ i ].ActionData + "Pass";
						//bool flag = isRefreshing ? TemplatesManager.Instance.SetOptionsValue( optionId, false ) : false;
						//if( !flag )
						//	owner.SetPassVisible( validActions[ i ].ActionData, false );
						TemplatesManager.Instance.SetOptionsValue( optionId , false ) ;
						owner.SetPassVisible( validActions[ i ].ActionData , false );
					}
					break;


					case AseOptionsActionType.IncludePass:
					{
						if( !uiItem.IsVisible )
							break;

						// @diogo: on refresh the saved Available Passes selection wins over the option's include
						if( isRefreshing )
						{
							break;
						}

						string optionId = validActions[ i ].ActionData + "Pass";
						TemplatesManager.Instance.SetOptionsValue( optionId, true );
						owner.SetPassVisible( validActions[ i ].ActionData, true );
					}
					break;
					case AseOptionsActionType.SetPropertyOnPass:
					{
						//Debug.Log( "PASSPROP " + validActions[ i ].ActionData );
						//Refresh happens on hotcode reload and shader load and in those situation
						// The property own serialization handles its setup
						if( isRefreshing && ( !owner.ApplyOptionPropertyActionsOnRefresh || !uiItem.IsVisible ) )
							continue;

						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								passMasterNode.SetPropertyActionFromItem( actionFromUser, passMasterNode.PassModule, validActions[ i ] );
							}
							else
							{
								LogMissingPass( owner, validActions[ i ] );
							}
						}
						else
						{
							owner.SetPropertyActionFromItem( actionFromUser, owner.PassModule, validActions[ i ] );
						}
					}
					break;
					case AseOptionsActionType.SetPropertyOnSubShader:
					{
						//Refresh happens on hotcode reload and shader load and in those situation
						// The property own serialization handles its setup
						if( isRefreshing && ( !owner.ApplyOptionPropertyActionsOnRefresh || !uiItem.IsVisible ) )
							continue;

						owner.SetPropertyActionFromItem( actionFromUser, owner.SubShaderModule, validActions[ i ] );
					}
					break;
					case AseOptionsActionType.SetShaderProperty:
					{
						//This action is only check when shader is compiled over
						//the TemplateMultiPassMasterNode via the on CheckPropertyChangesOnOptions() method
					}
					break;
					case AseOptionsActionType.ExcludeAllPassesBut:
					{
						//This action is only check when shader is compiled over
						//the TemplateMultiPassMasterNode via the on CheckExcludeAllPassOptions() method
					}
					break;
					case AseOptionsActionType.SetMaterialProperty:
					{
						if( isRefreshing )
							continue;

						if( !uiItem.IsVisible )
							break;

						if( owner.ContainerGraph.CurrentMaterial != null )
						{
							string prop = validActions[ i ].ActionData;
							if( owner.ContainerGraph.CurrentMaterial.HasProperty( prop ) )
							{
								if( uiItem.Options.UIWidget == AseOptionsUIWidget.Float || uiItem.Options.UIWidget == AseOptionsUIWidget.FloatRange )
									owner.ContainerGraph.CurrentMaterial.SetFloat( prop, uiItem.CurrentFieldValue );
								else
									owner.ContainerGraph.CurrentMaterial.SetInt( prop, (int)uiItem.CurrentFieldValue );

								if( MaterialInspector.Instance != null )
									MaterialInspector.Instance.Repaint();
							}
						}
					}
					break;
				}
			}
		}

		public void SetupCustomOptionsFromTemplate( TemplateMultiPassMasterNode owner, bool newTemplate )
		{
			TemplateOptionsContainer customOptionsContainer = m_isSubShader ? owner.SubShader.CustomOptionsContainer : owner.Pass.CustomOptionsContainer;

			if( !newTemplate && customOptionsContainer.Body.Length == m_passCustomOptionsSizeCheck )
			{
				for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
				{
					if( m_passCustomOptionsUI[ i ].EmptyEvent )
					{
						if( m_isSubShader )
						{
							m_passCustomOptionsUI[ i ].OnActionPerformedEvt += owner.OnCustomSubShaderOptionSelected;
						}
						else
						{
							m_passCustomOptionsUI[ i ].OnActionPerformedEvt += owner.OnCustomPassOptionSelected;
						}
					}
				}
				return;
			}

			m_passCustomOptionsLabel = string.IsNullOrEmpty( customOptionsContainer.Name ) ? CustomOptionsLabel : " " + customOptionsContainer.Name;

			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].Destroy();
			}

			m_passCustomOptionsUI.Clear();
			m_passCustomOptionsUIDict.Clear();
			m_passCustomOptionsPorts.Clear();

			if( customOptionsContainer.Enabled )
			{
				m_passCustomOptionsSizeCheck = customOptionsContainer.Body.Length;
				for( int i = 0; i < customOptionsContainer.Options.Length; i++ )
				{
					switch( customOptionsContainer.Options[ i ].Type )
					{
						case AseOptionsType.Option:
						{
							TemplateOptionUIItem item = new TemplateOptionUIItem( customOptionsContainer.Options[ i ] );
							if( m_isSubShader )
							{
								item.OnActionPerformedEvt += owner.OnCustomSubShaderOptionSelected;
							}
							else
							{
								item.OnActionPerformedEvt += owner.OnCustomPassOptionSelected;
							}

							m_passCustomOptionsUI.Add( item );
							m_passCustomOptionsUIDict.Add( customOptionsContainer.Options[ i ].Id, item );
						}
						break;
						case AseOptionsType.Port:
						{
							TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							m_passCustomOptionsPorts.Add( item );
							//if( m_isSubShader )
							//{
							//	if( string.IsNullOrEmpty( customOptionsContainer.Options[ i ].Id ) )
							//	{
							//		//No pass name selected. inject on all passes
							//		TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							//		m_passCustomOptionsPorts.Add( item );
							//	}
							//	else if( customOptionsContainer.Options[ i ].Id.Equals( owner.PassName ) )
							//	{
							//		TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							//		m_passCustomOptionsPorts.Add( item );
							//	}
							//}
							//else
							//{
							//	TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							//	m_passCustomOptionsPorts.Add( item );
							//}
						}
						break;
						case AseOptionsType.Field:
						{
							TemplateOptionUIItem item = new TemplateOptionUIItem( customOptionsContainer.Options[ i ] );
							if( m_isSubShader )
							{
								item.OnActionPerformedEvt += owner.OnCustomSubShaderOptionSelected;
							}
							else
							{
								item.OnActionPerformedEvt += owner.OnCustomPassOptionSelected;
							}

							m_passCustomOptionsUI.Add( item );
							m_passCustomOptionsUIDict.Add( customOptionsContainer.Options[ i ].Id, item );
						}
						break;
					}
				}
			}
			else
			{
				m_passCustomOptionsSizeCheck = 0;
			}
		}

		public void SetCustomOptionsInfo( TemplateMultiPassMasterNode masterNode, ref MasterNodeDataCollector dataCollector )
		{
			if( masterNode == null )
				return;

			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].FillDataCollector( ref dataCollector );
			}

			for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			{
				m_passCustomOptionsPorts[ i ].FillDataCollector( masterNode, ref dataCollector );
			}
		}

		public void CheckImediateActionsForPort( TemplateMultiPassMasterNode masterNode , int portId )
		{
			for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			{
				m_passCustomOptionsPorts[ i ].CheckImediateActionsForPort( masterNode, portId );
			}
		}

		public void SetSubShaderCustomOptionsPortsInfo( TemplateMultiPassMasterNode masterNode, ref MasterNodeDataCollector dataCollector  )
		{
			if( masterNode == null )
				return;


			//for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			//{
			//	if( string.IsNullOrEmpty( m_passCustomOptionsPorts[ i ].Options.Id ) ||
			//		masterNode.PassUniqueName.Equals( m_passCustomOptionsPorts[ i ].Options.Id ) )
			//	{
			//		m_passCustomOptionsPorts[ i ].FillDataCollector( masterNode, ref dataCollector );
			//	}
			//}

			for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			{
				m_passCustomOptionsPorts[ i ].SubShaderFillDataCollector( masterNode, ref dataCollector );
			}
		}

		public void RefreshCustomOptionsDict()
		{
			if( m_passCustomOptionsUIDict.Count != m_passCustomOptionsUI.Count )
			{
				m_passCustomOptionsUIDict.Clear();
				int count = m_passCustomOptionsUI.Count;
				for( int i = 0; i < count; i++ )
				{
					m_passCustomOptionsUIDict.Add( m_passCustomOptionsUI[ i ].Options.Id, m_passCustomOptionsUI[ i ] );
				}
			}
		}

		public void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			RefreshCustomOptionsDict();
			int savedOptions = Convert.ToInt32( nodeParams[ index++ ] );
			m_readOptions = new List<ReadOptions>();
			for( int i = 0; i < savedOptions; i++ )
			{
				string optionName = nodeParams[ index++ ];

				// @diogo: In cases like "Category,InvertActionOnDeselection", the actual name should only be the first
				//        part, to avoid conflicts with the template definition.
				optionName = optionName.Split( TemplateOptionsToolsHelper.OptionsDataSeparator )[ 0 ];

				string optionSelection = nodeParams[ index++ ];
				Int64 optionTimestamp = ( UIUtils.CurrentShaderVersion() > 18929 ) ? Convert.ToInt64( nodeParams[ index++ ] ):0;
				m_readOptions.Add( new ReadOptions() { Name = optionName , Selection = optionSelection , Timestamp = optionTimestamp });

			}
		}

		public void WriteToString( ref string nodeInfo )
		{
			int optionsCount = m_passCustomOptionsUI.Count;
			IOUtils.AddFieldValueToString( ref nodeInfo , optionsCount );
			for( int i = 0 ; i < optionsCount ; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo , m_passCustomOptionsUI[ i ].Options.Id );
				if( m_passCustomOptionsUI[ i ].Options.Type == AseOptionsType.Field )
					IOUtils.AddFieldValueToString( ref nodeInfo , m_passCustomOptionsUI[ i ].FieldValue.WriteToSingle() );
				else
					IOUtils.AddFieldValueToString( ref nodeInfo , m_passCustomOptionsUI[ i ].CurrentOption );

				IOUtils.AddFieldValueToString( ref nodeInfo , m_passCustomOptionsUI[ i ].LastClickedTimestamp );
			}

		}

		public void SetReadOptions()
		{
			if( m_readOptions != null )
			{
				for( int i = 0 ; i < m_readOptions.Count ; i++ )
				{
					if( m_passCustomOptionsUIDict.ContainsKey( m_readOptions[ i ].Name ) )
					{
						m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].LastClickedTimestamp = m_readOptions[ i ].Timestamp;
						if( m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].Options.Type == AseOptionsType.Field )
						{
							m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].FieldValue.ReadFromSingle( m_readOptions[ i ].Selection );

							// @diogo: option fields aren't tracked by InlinePropertyTable (their FieldValue lives on the
							//         shared/cached template option), so revert a dangling reference here, once the graph is loaded
							m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].FieldValue.RevertIfUnresolved();

							foreach( var item in m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].Options.ActionsPerOption.Rows )
							{
								if( item.Columns.Length > 0 && item.Columns[ 0 ].ActionType == AseOptionsActionType.SetMaterialProperty )
								{
									if( UIUtils.CurrentWindow.CurrentGraph.CurrentMaterial != null )
									{
										if( UIUtils.CurrentWindow.CurrentGraph.CurrentMaterial.HasProperty( item.Columns[ 0 ].ActionData ) )
										{
											m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].CurrentFieldValue = UIUtils.CurrentWindow.CurrentGraph.CurrentMaterial.GetFloat( item.Columns[ 0 ].ActionData );
										}
									}
								}
							}
						}
						else
							m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].CurrentOptionIdx = Convert.ToInt32( m_readOptions[ i ].Selection );
					}
				}
			}
		}

		// @diogo: applies only the saved non-field option selections, so the load-time reservation/visibility
		// cascade ( RegisterProperties ) runs from the saved state instead of template defaults. Field options
		// are left to the deferred SetReadOptions, whose RevertIfUnresolved needs the fully-loaded graph.
		public void SetReadOptionSelections()
		{
			if( m_readOptions == null )
				return;

			for( int i = 0 ; i < m_readOptions.Count ; i++ )
			{
				if( m_passCustomOptionsUIDict.ContainsKey( m_readOptions[ i ].Name ) )
				{
					TemplateOptionUIItem item = m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ];
					if( item.Options.Type != AseOptionsType.Field )
					{
						item.LastClickedTimestamp = m_readOptions[ i ].Timestamp;
						item.CurrentOptionIdx = Convert.ToInt32( m_readOptions[ i ].Selection );
					}
				}
			}
		}

		public void Refresh()
		{
			//int count = m_passCustomOptionsUI.Count;
			//for( int i = 0; i < count; i++ )
			//{
			//	m_passCustomOptionsUI[ i ].Refresh();
			//}
			List<TemplateOptionUIItem> sortedList = m_passCustomOptionsUI.OrderBy( item => item.LastClickedTimestamp ).ToList();
			int count = sortedList.Count;
			for( int i = 0 ; i < count ; i++ )
			{
				sortedList[ i ].Update();
			}
		}

		public void CheckDisable()
		{
			int count = m_passCustomOptionsUI.Count;
			for( int i = 0; i < count; i++ )
			{
				m_passCustomOptionsUI[ i ].CheckEnDisable( false, false );
			}
		}

		public List<TemplateOptionUIItem> PassCustomOptionsUI { get { return m_passCustomOptionsUI; } }
	}
}
