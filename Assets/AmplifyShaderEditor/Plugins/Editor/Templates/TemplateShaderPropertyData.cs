// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateShaderPropertyData
	{
		public string PropertyInspectorName;
		public string PropertyName;
		public WirePortDataType PropertyDataType;
		public PropertyType PropertyType;
		public PrecisionType PrecisionType;

		public int Index;
		public string FullValue;
		public string ReplacementValueHelper;
		public string Identation;

		// True when the property is commented out in the template (a conditional/optional declaration),
		// meaning the template does not currently emit it on the Properties block
		public bool Commented;

		// True when a pass body declares this property's uniform outside any preprocessor conditional,
		// meaning the uniform exists even while the property is commented out on the Properties block.
		// Such uniforms must always be soft registered so user properties reusing the name don't
		// redeclare them; it does not reserve the name (users may alias these, e.g. _AlphaClip)
		public bool UniformDeclaredUnconditionally;

		// The template holds this name only while the property is active on the Properties block;
		// commented properties leave their names free for users to declare
		public bool NameInUse { get { return !Commented; } }

		public bool IsMacro;

		// True when the template declares this texture with a legacy sampler type (sampler2D and
		// friends) rather than a texture object, so it can only be sampled through texXD intrinsics
		public bool DeclaredAsLegacySampler;

		public int SubShaderId;
		public int PassId;

		public TemplateShaderPropertyData( int index, string fullValue, string identation, string propertyInspectorName, string propertyName, WirePortDataType propertyDataType, PropertyType propertyType,int subShaderId, int passId, bool isMacro = false, PrecisionType precisionType = PrecisionType.Float )
		{
			Index = index;
			FullValue = fullValue;
			Identation = identation;
			PropertyInspectorName = string.IsNullOrEmpty( propertyInspectorName )?propertyName: propertyInspectorName;
			PropertyName = propertyName;
			PropertyDataType = propertyDataType;
			PropertyType = propertyType;
			int idx = FullValue.LastIndexOf( "=" );
			ReplacementValueHelper = ( idx >= 0 ) ? FullValue.Substring( 0, idx + 1 ) +" ": FullValue + " = ";
			IsMacro = isMacro;
			SubShaderId = subShaderId;
			PassId = passId;
			PrecisionType = precisionType;
		}

		public string CreatePropertyForValue( string value )
		{
			return value.Contains( PropertyName ) ? Identation + value : ReplacementValueHelper + value;
		}

		public override string ToString()
		{
			return string.Format( "{0}(\"{1}\", {2})", PropertyName, PropertyInspectorName,UIUtils.WirePortToCgType( PropertyDataType ) );
		}
	}
}
