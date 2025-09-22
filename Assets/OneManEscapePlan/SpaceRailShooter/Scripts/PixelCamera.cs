/// © 2018-2019 Kevin Foley
/// For distribution only on the Unity Asset Store
/// Terms/EULA: https://unity3d.com/legal/as_terms

using OneManEscapePlan.SpaceRailShooter.Scripts.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace OneManEscapePlan.SpaceRailShooter.Scripts.Effects {

	/// <summary>
	/// Renders the scene using the specified settings file. We use [ExecuteInEditMode] so that changes
	/// we make to the render settings file show up in real-time.
	/// </summary>
	/// 
	/// COMPLEXITY: Advanced
	/// CONCEPTS: Render textures, Post-processing, Blitting, Upscaling
	/// 
	/// https://docs.unity3d.com/Manual/class-RenderTexture.html
	/// https://docs.unity3d.com/ScriptReference/Graphics.Blit.html
	/// https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnRenderImage.html
	/// 
	/// When custom render settings are enabled, this class configures the camera so that the image is
	/// rendered to a render texture at the desired render resolution. The rendered image is then
	/// blitted into the display backbuffer to be displayed at the actual window resolution. This is
	/// necessary if we want to render at a very low retro-resolution (e.g. 256x224) without blurring.
	/// 
	/// Normally, when Unity is rendered at a lower resolution than the monitor, the image is upscaled
	/// with bilinear filtering, which can make the image extremely blurry if there is a significant
	/// difference between the render resolution and the monitor/window resolution. Blitting from a 
	/// RenderTexture to the display backbuffer allows us to use point (nearest-neighbor) filtering,
	/// which preserves the sharp pixels we desire.
	/// 
	/// If the custom render settings are disabled, the camera renders at the native resolution of the
	/// monitor/window.
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	public class PixelCamera : MonoBehaviour {
		[Tooltip("An SRSRenderSettings asset that defines our desired render settings (required)")]
		[SerializeField] protected SRSRenderSettings renderSettings;
		[Tooltip("Set a custom material to use for the blitting process (not required; I recommend leaving this null)")]
		[SerializeField] protected Material renderMaterial;
		protected RenderTexture rt;
		new Camera camera;

		public SRSRenderSettings RenderSettings => renderSettings;
		public Camera Camera => camera;
		
		// Use this for initialization
		void Start() {
			if (Application.isPlaying) {
				Assert.IsNotNull<SRSRenderSettings>(renderSettings, "You forgot to select a RenderSettings file");
			}
			if (renderSettings == null) return;

			camera = GetComponent<Camera>();

			if (renderSettings.UseCustomSettings) {
				rt = renderSettings.RenderTexture;
				Assert.IsNotNull<RenderTexture>(rt);
				camera.targetTexture = rt;

				updateSettings();
			} else {
				camera.targetTexture = null;
				Application.targetFrameRate = -1; //use default setting for the current platform
			}
		}

		/// <summary>
		/// Apply settings from the Render Settings asset
		/// </summary>
		private void updateSettings() {
			if (renderSettings == null || !renderSettings.UseCustomSettings || rt == null) return;

			if (Screen.width == 0 || Screen.height == 0) {
				Debug.Log("Screen size is 0; this can happen when the Editor first launches.");
				return;
			}
			float aspect = Screen.width / (float)Screen.height;
			int targetWidth = Mathf.FloorToInt(rt.height * aspect);
			if (rt.width != targetWidth) {
				rt.Release();
				rt.width = targetWidth;
				Debug.Log("Set RenderTexture width to " + rt.width + ", aspect " + aspect);
			}
			if (Application.targetFrameRate != renderSettings.Framerate) {
				Application.targetFrameRate = renderSettings.Framerate;
			}
		}

		private void OnPreRender() {
			if (renderSettings != null && renderSettings.UseCustomSettings && camera != null) {
				//constantly check settings in the editor so we can see render-settings changes in real-time
#if UNITY_EDITOR
				updateSettings();
#endif
				//we null the camera's targetTexture in OnRenderImage(), and must re-apply it here
				camera.targetTexture = rt;
			}
		}

		//see https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnRenderImage.html
		private void OnRenderImage(RenderTexture source, RenderTexture destination) {
			if (camera == null) return;
			camera.targetTexture = null; //this is necessary so that our rendered image actually appears onscreen

			 //copy our rendered image from the RenderTexture to the display backbuffer. this resizes the rendered image without blurring it
			source.filterMode = FilterMode.Point;
			blit(source, null);
		}

		private void blit(Texture source, RenderTexture destination) {
			if (renderMaterial != null) Graphics.Blit(source, destination, renderMaterial);
			else Graphics.Blit(source, destination);
		}
	}
}