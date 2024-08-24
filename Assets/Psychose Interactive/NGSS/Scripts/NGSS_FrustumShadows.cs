using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PI.NGSS
{
    [ImageEffectAllowedInSceneView]
    [ExecuteInEditMode]
    public class NGSS_FrustumShadows : MonoBehaviour
    {
        //2.3.9
        [Header("REFERENCES")] public Light mainShadowsLight;
        public Shader frustumShadowsShader;

        [Header("SHADOWS SETTINGS")] [Tooltip("Poisson Noise. Randomize samples to remove repeated patterns.")]
        public bool m_dithering = false;

        [Tooltip("If enabled a faster separable blur will be used.\nIf disabled a slower depth aware blur will be used.")]
        public bool m_fastBlur = true;

        [Tooltip("If enabled, backfaced lit fragments will be skipped increasing performance. Requires GBuffer normals.")]
        public bool m_deferredBackfaceOptimization = false;

        [Range(0f, 1f), Tooltip("Set how backfaced lit fragments are shaded. Requires DeferredBackfaceOptimization to be enabled.")]
        public float m_deferredBackfaceTranslucency = 0f;

        [Tooltip("Tweak this value to remove soft-shadows leaking around edges.")] [Range(0.01f, 1f)]
        public float m_shadowsEdgeBlur = 0.25f;

        [Tooltip("Overall softness of the shadows.")] [Range(0.01f, 1f)]
        public float m_shadowsBlur = 0.5f;

        [Tooltip("Overall softness of the shadows. Higher values than 1 wont work well if FastBlur is enabled.")] [Range(1, 4)]
        public int m_shadowsBlurIterations = 1;

        [Tooltip("Rising this value will make shadows more blurry but also lower in resolution.")] [Range(1, 4)]
        public int m_shadowsDownGrade = 1;

        //[Tooltip("The distance where shadows start to fade.")]
        //[Range(0.1f, 4.0f)]
        //public float m_shadowsFade = 1f;

        [Tooltip("Tweak this value if your objects display backface shadows.")] [Range(0.0f, 1f)]
        public float m_shadowsBias = 0.05f;

#if !UNITY_5
        [Tooltip("The distance in metters from camera where shadows start to shown.")]
        //[Min(0f)]
        public float m_shadowsDistanceStart = 0f;
#else
    [Tooltip("The distance in metters from camera where shadows start to shown.")]
    public float m_shadowsDistanceStart = 0f;
#endif
        [Header("RAY SETTINGS")] [Tooltip("If enabled the ray length will be scaled at screen space instead of world space. Keep it enabled for an infinite view shadows coverage. Disable it for a ContactShadows like effect. Adjust the Ray Scale property accordingly.")]
        public bool m_rayScreenScale = true;

        [Tooltip("Number of samplers between each step. The higher values produces less gaps between shadows but is more costly.")] [Range(16, 128)]
        public int m_raySamples = 64;

        [Tooltip("The higher the value, the larger the shadows ray will be.")] [Range(0.01f, 1f)]
        public float m_rayScale = 0.25f;

        [Tooltip("The higher the value, the ticker the shadows will look.")] [Range(0.0f, 1.0f)]
        public float m_rayThickness = 0.01f;

        [Header("TEMPORAL SETTINGS")] [Tooltip("Enable this option if you use temporal anti-aliasing in your project. Works better when Dithering is enabled.")]
        public bool m_Temporal;

        [Range(0f, 1f)] public float m_JitterScale = 0.5f;

        /*******************************************************************************************************************/

        //private Texture2D noMixTexture;
        private int m_temporalJitter;
        private int _iterations = 1;
        private int _downGrade = 1;
        private int _width;
        private int _height;

        //private int _sampleIndex = 0;
        private RenderingPath _currentRenderingPath = RenderingPath.VertexLit;
        private CommandBuffer computeShadowsCB;
        private bool _isInit;

        bool IsNotSupported()
        {
#if UNITY_2018_1_OR_NEWER
            return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2);
#elif UNITY_2017_4_OR_EARLIER
        return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStationVita || SystemInfo.graphicsDeviceType == GraphicsDeviceType.N3DS);
#else
        return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D9 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStationMobile || SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStationVita || SystemInfo.graphicsDeviceType == GraphicsDeviceType.N3DS);
#endif
        }

        private Camera _mCamera;

        private Camera mCamera
        {
            get
            {
                if (_mCamera == null)
                {
                    _mCamera = GetComponent<Camera>();
                    if (_mCamera == null)
                    {
                        _mCamera = Camera.main;
                    }

                    if (_mCamera == null)
                    {
                        Debug.LogError("NGSS Error: No MainCamera found, please provide one.", this);
                        enabled = false;
                    }
                    //#if UNITY_EDITOR
                    //if (UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera != null)
                    //UnityEditor.SceneView.currentDrawingSceneView.camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
                    //#endif

                    //MonoBehaviour ngss_cs = _mCamera.GetComponent("NGSS_ContactShadows") as MonoBehaviour;
                    //if (ngss_cs != null) { ngss_cs.enabled = false; DestroyImmediate(ngss_cs); }
                }

                return _mCamera;
            }
        }

        private Material _mMaterial;

        private Material mMaterial
        {
            set => _mMaterial = value;
            get
            {
                if (_mMaterial == null)
                {
                    //_mMaterial = new Material(Shader.Find("Hidden/NGSS_FrustumShadows"));//Automatic (sometimes it bugs)
                    if (frustumShadowsShader == null)
                    {
                        frustumShadowsShader = Shader.Find("Hidden/NGSS_FrustumShadows");
                    }

                    _mMaterial = new Material(frustumShadowsShader); //Manual
                    if (_mMaterial == null)
                    {
                        Debug.LogWarning("NGSS Warning: can't find NGSS_FrustumShadows shader, make sure it's on your project.", this);
                        enabled = false;
                    }
                }

                return _mMaterial;
            }
        }

        void AddCommandBuffers()
        {
            if (computeShadowsCB == null)
            {
                computeShadowsCB = new CommandBuffer {name = "NGSS FrustumShadows: Compute"};
            }
            else
            {
                computeShadowsCB.Clear();
            }

            var canAddBuff = true;
            if (mCamera)
            {
                foreach (CommandBuffer cb in mCamera.GetCommandBuffers(mCamera.actualRenderingPath == RenderingPath.DeferredShading ? CameraEvent.BeforeLighting : CameraEvent.AfterDepthTexture))
                {
                    if (cb.name != computeShadowsCB.name) continue;
                    canAddBuff = false;
                    break;
                }

                if (canAddBuff)
                {
                    mCamera.AddCommandBuffer(mCamera.actualRenderingPath == RenderingPath.DeferredShading ? CameraEvent.BeforeLighting : CameraEvent.AfterDepthTexture, computeShadowsCB);
                }
            }
            /*
    #if UNITY_EDITOR
            if (UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera != null)
            {
                UnityEditor.SceneView.currentDrawingSceneView.camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
                UnityEditor.SceneView.currentDrawingSceneView.camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, computeShadowsCB);
            }
    #endif*/
        }

        void RemoveCommandBuffers()
        {
            _mMaterial = null;
            if (mCamera)
            {
                mCamera.RemoveCommandBuffer(CameraEvent.BeforeLighting, computeShadowsCB);
                mCamera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, computeShadowsCB);
            }

            //We dont need this anymore as the contact shadows mix is done directly on shadow internal files
            /*
    #if UNITY_EDITOR
            if (UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera != null)
                UnityEditor.SceneView.currentDrawingSceneView.camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, computeShadowsCB);
    #endif*/

            _isInit = false;
        }

        void Init()
        {
            var w = UnityEngine.XR.XRSettings.enabled ? UnityEngine.XR.XRSettings.eyeTextureWidth : mCamera.scaledPixelWidth;
            var h = UnityEngine.XR.XRSettings.enabled ? UnityEngine.XR.XRSettings.eyeTextureHeight : mCamera.scaledPixelHeight;

            m_shadowsBlurIterations = m_fastBlur ? 1 : m_shadowsBlurIterations;

            if (_iterations == m_shadowsBlurIterations && _downGrade == m_shadowsDownGrade && _width == w && _height == h && (_isInit || mainShadowsLight == null))
            {
                return;
            }

            //comment me these 3 lines if sampling directly from internal files
            //noMixTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            //noMixTexture.SetPixel(0, 0, Color.white);
            //noMixTexture.Apply();

            if (mCamera.actualRenderingPath == RenderingPath.VertexLit)
            {
                Debug.LogWarning("Vertex Lit Rendering Path is not supported by NGSS Contact Shadows. Please set the Rendering Path in your game camera or Graphics Settings to something else than Vertex Lit.", this);
                enabled = false;
                //DestroyImmediate(this);
                return;
            }
            else if (mCamera.actualRenderingPath == RenderingPath.Forward)
            {
                mCamera.depthTextureMode |= DepthTextureMode.Depth;
            }

            AddCommandBuffers();

            //computeShadowsCB.GetTemporaryRT(cShadow, UnityEngine.XR.XRSettings.eyeTextureDesc, FilterMode.Bilinear);
            _width = w;
            _height = h;
            _downGrade = m_shadowsDownGrade;

            var cShadow1 = Shader.PropertyToID("NGSS_ContactShadowRT1");
            var cShadow2 = Shader.PropertyToID("NGSS_ContactShadowRT2");
            //var dSource = Shader.PropertyToID("NGSS_DepthSourceRT");

            computeShadowsCB.GetTemporaryRT(cShadow1, w / _downGrade, h / _downGrade, 0, FilterMode.Bilinear, RenderTextureFormat.RG16);
            computeShadowsCB.GetTemporaryRT(cShadow2, w / _downGrade, h / _downGrade, 0, FilterMode.Bilinear, RenderTextureFormat.RG16);
            //computeShadowsCB.SetGlobalTexture(Shader.PropertyToID("ScreenSpaceMask"), BuiltinRenderTextureType.CurrentActive);//requires a commandBuffer on the light, not compatible with local light

            computeShadowsCB.Blit(null, cShadow1, mMaterial, 0); //compute ssrt shadows

            //blur shadows
            _iterations = m_shadowsBlurIterations;
            for (var i = 1; i <= _iterations; i++)
            {
                computeShadowsCB.SetGlobalVector("ShadowsKernel", new Vector2(0f, i));
                computeShadowsCB.Blit(cShadow1, cShadow2, mMaterial, 1);
                computeShadowsCB.SetGlobalVector("ShadowsKernel", new Vector2(i, 0f));
                computeShadowsCB.Blit(cShadow2, cShadow1, mMaterial, 1);
            }

            computeShadowsCB.SetGlobalTexture("NGSS_FrustumShadowsTexture", cShadow1);


            computeShadowsCB.ReleaseTemporaryRT(cShadow1);
            computeShadowsCB.ReleaseTemporaryRT(cShadow2);
            //computeShadowsCB.ReleaseTemporaryRT(dSource);

            _isInit = true;
        }

        /******************************************************************/

        void OnEnable()
        {
            if (IsNotSupported())
            {
                Debug.LogWarning("Unsupported graphics API, NGSS requires at least SM3.0 or higher and DX9 is not supported.", this);
                enabled = false;
                return;
            }

            Init();
        }

        void OnDisable()
        {
            Shader.SetGlobalFloat("NGSS_FRUSTUM_SHADOWS_ENABLED", 0f);

            if (_isInit)
            {
                RemoveCommandBuffers();
            }

            if (mMaterial != null)
            {
                DestroyImmediate(mMaterial);
                mMaterial = null;
            }

            //mCamera.depthTextureMode &= ~(DepthTextureMode.MotionVectors);
            //#if UNITY_EDITOR
            //if (UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera != null)
            //UnityEditor.SceneView.currentDrawingSceneView.camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            //#endif
        }

        void OnApplicationQuit()
        {
            if (_isInit)
            {
                RemoveCommandBuffers();
            }
        }

        /******************************************************************/
        private void OnPostRender()
        {
            Shader.SetGlobalFloat("NGSS_FRUSTUM_SHADOWS_ENABLED", 0f);
        }

        void OnPreRender()
        {
            Init();
            if (_isInit == false || mainShadowsLight == null)
            {
                return;
            }

            if (_currentRenderingPath != mCamera.actualRenderingPath)
            {
                _currentRenderingPath = mCamera.actualRenderingPath;
                RemoveCommandBuffers();
                AddCommandBuffers();
            }

            Shader.SetGlobalFloat("NGSS_FRUSTUM_SHADOWS_ENABLED", 1f);
            Shader.SetGlobalFloat("NGSS_FRUSTUM_SHADOWS_OPACITY", 1f - mainShadowsLight.shadowStrength);

            if (m_Temporal)
            {
                m_temporalJitter = (m_temporalJitter + 1) % 8;
                mMaterial.SetFloat("TemporalJitter", m_temporalJitter * m_JitterScale * 0.0002f);
            }
            else
            {
                mMaterial.SetFloat("TemporalJitter", 0f);
            }

            //mMaterial.SetMatrix("InverseProj", Matrix4x4.Inverse(mCamera.projectionMatrix));//proj to cam        
            //mMaterial.SetMatrix("InverseView", mCamera.cameraToWorldMatrix);//cam to world        
            //mMaterial.SetMatrix("InverseViewProj", Matrix4x4.Inverse(GL.GetGPUProjectionMatrix(mCamera.projectionMatrix, false) * mCamera.worldToCameraMatrix));//proj to world     
            if (QualitySettings.shadowProjection == ShadowProjection.StableFit)
            {
                mMaterial.EnableKeyword("SHADOWS_SPLIT_SPHERES");
            }
            else
            {
                mMaterial.DisableKeyword("SHADOWS_SPLIT_SPHERES");
            }

            mMaterial.SetMatrix("WorldToView", mCamera.worldToCameraMatrix); //cam to world        
            mMaterial.SetVector("LightDir", mCamera.transform.InverseTransformDirection(-mainShadowsLight.transform.forward)); //view space direction
            mMaterial.SetVector("LightPosRange", new Vector4(mainShadowsLight.transform.position.x, mainShadowsLight.transform.position.y, mainShadowsLight.transform.position.z, mainShadowsLight.range * mainShadowsLight.range)); //world position        
            mMaterial.SetVector("LightDirWorld", -mainShadowsLight.transform.forward); //view space direction
            //mMaterial.SetFloat("ShadowsOpacity", 1f - mainShadowsLight.shadowStrength);        
            mMaterial.SetFloat("ShadowsEdgeTolerance", m_shadowsEdgeBlur);
            mMaterial.SetFloat("ShadowsSoftness", m_shadowsBlur);
            //mMaterial.SetFloat("ShadowsDistance", m_shadowsDistance);
            mMaterial.SetFloat("RayScale", m_rayScale);
            //mMaterial.SetFloat("ShadowsFade", m_shadowsFade);
            mMaterial.SetFloat("ShadowsBias", m_shadowsBias * 0.02f);
            mMaterial.SetFloat("ShadowsDistanceStart", m_shadowsDistanceStart - 10f);
            mMaterial.SetFloat("RayThickness", m_rayThickness);
            mMaterial.SetFloat("RaySamples", m_raySamples);
            //mMaterial.SetFloat("RaySamplesScale", m_raySamplesScale);
            if (m_deferredBackfaceOptimization && mCamera.actualRenderingPath == RenderingPath.DeferredShading)
            {
                mMaterial.EnableKeyword("NGSS_DEFERRED_OPTIMIZATION");
                mMaterial.SetFloat("BackfaceOpacity", m_deferredBackfaceTranslucency);
            }
            else
            {
                mMaterial.DisableKeyword("NGSS_DEFERRED_OPTIMIZATION");
            }

            if (m_dithering)
            {
                mMaterial.EnableKeyword("NGSS_USE_DITHERING");
            }
            else
            {
                mMaterial.DisableKeyword("NGSS_USE_DITHERING");
            }

            if (m_fastBlur)
            {
                mMaterial.EnableKeyword("NGSS_FAST_BLUR");
            }
            else
            {
                mMaterial.DisableKeyword("NGSS_FAST_BLUR");
            }

            if (mainShadowsLight.type != LightType.Directional)
            {
                mMaterial.EnableKeyword("NGSS_USE_LOCAL_SHADOWS");
            }
            else
            {
                mMaterial.DisableKeyword("NGSS_USE_LOCAL_SHADOWS");
            }

            //if (m_rayScreenScale) { mMaterial.EnableKeyword("NGSS_RAY_SCREEN_SCALE"); } else { mMaterial.DisableKeyword("NGSS_RAY_SCREEN_SCALE"); }
            mMaterial.SetFloat("RayScreenScale", m_rayScreenScale ? 1f : 0f);
        }
        //uncomment me if using the screen space blit | comment me if sampling directly from internal NGSS libraries
        /*void OnPostRender()
        {
            Shader.SetGlobalFloat("NGSS_FRUSTUM_SHADOWS_ENABLED", 0f);
            //Shader.SetGlobalTexture("NGSS_FrustumShadowsTexture", noMixTexture);//don't render shadows
        }*/

        /******************************************************************/
        //Helpers

        void BlitXR(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dest, Material mat, int pass)
        {
            cmd.SetRenderTarget(dest, 0, CubemapFace.Unknown, -1);
            cmd.ClearRenderTarget(true, true, Color.clear);
            //cmd.SetGlobalTexture(mainTexID, src);
            cmd.DrawMesh(FullScreenTriangle, Matrix4x4.identity, mat, pass);
        }

        private Mesh _fullScreenTriangle;

        private Mesh FullScreenTriangle
        {
            get
            {
                if (_fullScreenTriangle) return _fullScreenTriangle;

                _fullScreenTriangle = new Mesh
                {
                    name = "Full-Screen Triangle",
                    vertices = new Vector3[]
                    {
                        new Vector3(-1f, -1f, 0f),
                        new Vector3(-1f, 3f, 0f),
                        new Vector3(3f, -1f, 0f)
                    },
                    triangles = new int[] {0, 1, 2},
                };

                _fullScreenTriangle.UploadMeshData(true);

                return _fullScreenTriangle;
            }
        }
    }
}