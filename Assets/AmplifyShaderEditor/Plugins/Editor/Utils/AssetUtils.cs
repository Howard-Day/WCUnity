// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;

namespace AmplifyShaderEditor
{
#if UNITY_6000_4_OR_NEWER
	using EndActionClassType = AssetCreationEndAction;
	using EntityIdType = EntityId;
#else
	using EndActionClassType = EndNameEditAction;
	using EntityIdType = System.Int32;
#endif

	public class AssetUtils
	{
		public struct EntityId
		{
			public readonly EntityIdType Value;
			public EntityId( EntityIdType value ) { Value = value; }
			public static EntityIdType None => default( EntityIdType );
		}

		public abstract class EndAction : EndActionClassType
		{
			public virtual void Action( EntityId entityId, string pathName, string resourceFile )
			{
			}

			public override void Action( EntityIdType entityId, string pathName, string resourceFile )
			{
				Action( new EntityId( entityId ), pathName, resourceFile );
			}
		}

		public static UnityEngine.Object EntityIdToObject( AssetUtils.EntityId entityId )
		{
		#if UNITY_6000_3_OR_NEWER
			return EditorUtility.EntityIdToObject( entityId.Value );
		#else
			return EditorUtility.InstanceIDToObject( entityId.Value );
		#endif
		}

		public static EntityIdType GetEntityId( ScriptableObject asset )
		{
		#if UNITY_6000_4_OR_NEWER
			return asset.GetEntityId();
		#else
			return asset.GetInstanceID();
		#endif
		}

		public static void ImportPackage( string packagePath, bool interactive )
		{
		#if UNITY_6000_6_OR_NEWER
			UnityEditor.AssetPackage.Package.Import( packagePath, interactive );
		#else
			AssetDatabase.ImportPackage( packagePath, interactive );
		#endif
		}

		public static void ExportPackage( string[] assetPathNames, string fileName, ExportPackageOptions flags )
		{
		#if UNITY_6000_6_OR_NEWER
			UnityEditor.AssetPackage.Package.Export( new UnityEditor.AssetPackage.ExportPackageParameters( assetPathNames, fileName, string.Empty, flags ) );
		#else
			AssetDatabase.ExportPackage( assetPathNames, fileName, flags );
		#endif
		}
	}
}
