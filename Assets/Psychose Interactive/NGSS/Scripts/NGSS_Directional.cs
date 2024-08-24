using UnityEngine;
using UnityEngine.Rendering;

namespace PI.NGSS
{
    [RequireComponent(typeof(Light))]
    [ExecuteInEditMode()]
    public class NGSS_Directional : MonoBehaviour
    {
        [Header("MAIN SETTINGS")] public Shader denoiserShader;

        [Tooltip("If disabled, NGSS Directional shadows replacement will be removed from Graphics settings when OnDisable is called in this component.")]
        public bool NGSS_KEEP_ONDISABLE = true;

        [Tooltip("Check this option if you don't need to update shadows variables at runtime, only once when scene loads.\nUseful to save some CPU cycles.")]
        public bool NGSS_NO_UPDATE_ON_PLAY = false;

        //[Tooltip("Useful if you want to fallback to hard shadows at runtime without having to disable the component.")]
        //public bool NGSS_FORCE_HARD_SHADOWS = false;
        public enum ShadowMapResolution
        {
            UseQualitySettings = 256,
            VeryLow = 512,
            Low = 1024,
            Med = 2048,
            High = 4096,
            Ultra = 8192,
            Mega = 16384
        }

        [Tooltip("Shadows resolution.\nUseQualitySettings = From Quality Settings, SuperLow = 512, Low = 1024, Med = 2048, High = 4096, Ultra = 8192, Mega = 16384.")]
        public ShadowMapResolution NGSS_SHADOWS_RESOLUTION = ShadowMapResolution.UseQualitySettings;

        [Header("BASE SAMPLING")] [Tooltip("Used to test blocker search and early bail out algorithms. Keep it as low as possible, might lead to white noise if too low.\nRecommended values: Mobile = 8, Consoles & VR = 16, Desktop = 24")] [Range(4, 32)]
        public int NGSS_SAMPLING_TEST = 16;
        //public enum samplingType { BOX = 1, GAUSSIAN = 2, ROTATED_DISK = 3 }
        //[Tooltip("Sampling type used when filtering the shadows. BOX = Classic linear interpolation, GAUSSIAN = Gauss function interpolation, Disk = Random rotated samplers in a disk.")]
        //public samplingType NGSS_SAMPLING_TYPE = samplingType.ROTATED_DISK;

        [Tooltip("Number of samplers per pixel used for PCF and PCSS shadows algorithms.\nRecommended values: Mobile = 16, Consoles & VR = 32, Desktop Med = 48, Desktop High = 64, Desktop Ultra = 128")] [Range(8, 128)]
        public int NGSS_SAMPLING_FILTER = 48;

        [Tooltip("New optimization that reduces sampling over distance. Interpolates current sampling set (TEST and FILTER) down to 4spp when reaching this distance.")] [Range(0f, 500f)]
        public float NGSS_SAMPLING_DISTANCE = 75f;

        [Header("SHADOW SOFTNESS")] [Tooltip("Overall shadows softness.")] [Range(0f, 3f)]
        public float NGSS_SHADOWS_SOFTNESS = 1f;

        //Unity5 does not have Inline sampling so PCSS disabled by default in Unity5
#if !UNITY_5
        [Header("PCSS")] [Tooltip("PCSS Requires inline sampling and SM3.5.\nProvides Area Light soft-shadows.\nDisable it if you are looking for PCF filtering (uniform soft-shadows) which runs with SM3.0.")]
        public bool NGSS_PCSS_ENABLED = false;

        [Tooltip("How soft shadows are when close to caster.")] [Range(0f, 2f)]
        public float NGSS_PCSS_SOFTNESS_NEAR = 0.125f;

        [Tooltip("How soft shadows are when far from caster.")] [Range(0f, 2f)]
        public float NGSS_PCSS_SOFTNESS_FAR = 1f;
#endif


        [Header("NOISE")] [Tooltip("If zero = 100% noise.\nIf one = 100% dithering.\nUseful when fighting banding.")] [Range(0, 1)]
        public int NGSS_NOISE_TO_DITHERING_SCALE = 0;

        //[Tooltip("Distance at which shadows noise will fade out. Interpolates from noise to banding over this distance.")]
        //[Range(0f, 500f)]
        //public float NGSS_NOISE_DISTANCE = 15f;
        [Tooltip("If you set the noise scale value to something less than 1 you need to input a noise texture.\nRecommended noise textures are blue noise signals.")]
        public Texture2D NGSS_NOISE_TEXTURE;


        [Header("DENOISER")] [Tooltip("Separable low pass filter that help fight artifacts and noise in shadows.\nRequires Cascaded Shadows to be enabled in the Editor Graphics Settings.")]
        public bool NGSS_DENOISER_ENABLED = true;

        [Tooltip("How many iterations the Denoiser algorithm should do.")] [Range(1, 4)]
        public int NGSS_DENOISER_PASSES = 1;

        [Tooltip("Overall Denoiser softness.")] [Range(0.01f, 1f)]
        public float NGSS_DENOISER_SOFTNESS = 1f;

        [Tooltip("The amount of shadow edges the Denoiser can tolerate during denoising.")] [Range(0.01f, 1f)]
        public float NGSS_DENOISER_EDGE_SOFTNESS = 1f;

        [Header("BIAS")] [Tooltip("This estimates receiver slope using derivatives and tries to tilt the filtering kernel along it.\nHowever, when doing it in screenspace from the depth texture can leads to shadow artifacts.\nThus it is disabled by default.")]
        public bool NGSS_RECEIVER_PLANE_BIAS = false;
        //[Tooltip("Minimal fractional error for the receiver plane bias algorithm.")]
        //[Range(0f, 0.1f)]
        //public float NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR = 0.01f;
        //[Tooltip("Minimal fractional error for the receiver plane bias algorithm.")]
        //[Range(0.01f, 2f)]
        //public float NGSS_RECEIVER_PLANE_BIAS_WEIGHT = 1f;    
        //[Tooltip("Fades out artifacts produced by shadow bias")]
        //public bool NGSS_BIAS_FADE = true;
        //[Tooltip("Fades out artifacts produced by shadow bias")]
        //[Range(0f, 2f)]
        //public float NGSS_BIAS_FADE_VALUE = 1f;


        [Header("CASCADES")] [Tooltip("Blends cascades at seams intersection.\nAdditional overhead required for this option.")]
        public bool NGSS_CASCADES_BLENDING = true;

        [Tooltip("Tweak this value to adjust the blending transition between cascades.")] [Range(0f, 2f)]
        public float NGSS_CASCADES_BLENDING_VALUE = 1f;

        [Range(0f, 1f)] [Tooltip("If one, softness across cascades will be matched using splits distribution, resulting in realistic soft-ness over distance.\nIf zero the softness distribution will be based on cascade index, resulting in blurrier shadows over distance thus less realistic.")]
        public float NGSS_CASCADES_SOFTNESS_NORMALIZATION = 1f;

        //[Header("GLOBAL SETTINGS")]
        //[Tooltip("Enable it to let NGSS_Directional control global shadows settings through this component.\nDisable it if you want to manage shadows settings through Unity Quality & Graphics Settings panel.")]
        //private bool GLOBAL_SETTINGS_OVERRIDE = false;

        //[Tooltip("Shadows projection.\nRecommeded StableFit as it helps stabilizing shadows as camera moves.")]
        //private ShadowProjection GLOBAL_SHADOWS_PROJECTION = ShadowProjection.StableFit;

        //[Tooltip("Sets the maximum distance at wich shadows are visible from camera.\nThis option affects your shadow distance in Quality Settings.")]
        //private float GLOBAL_SHADOWS_DISTANCE = 150f;

        //[Range(0, 4)]
        //[Tooltip("Number of cascades the shadowmap will have. This option affects your cascade counts in Quality Settings.\nYou should entierly disable Cascaded Shadows (Graphics Menu) if you are targeting low-end devices.")]
        //private int GLOBAL_CASCADES_COUNT = 4;

        //[Range(0.01f, 0.25f)]
        //[Tooltip("Used for the cascade stitching algorithm.\nCompute cascades splits distribution exponentially in a x*2^n form.\nIf 4 cascades, set this value to 0.1. If 2 cascades, set it to 0.25.\nThis option affects your cascade splits in Quality Settings.")]
        //private float GLOBAL_CASCADES_SPLIT_VALUE = 0.1f;

        /****************************************************************/

        //DIR LIGHT
        private int _ngssDirSamplingDistanceid = Shader.PropertyToID("NGSS_DIR_SAMPLING_DISTANCE");
        private int _ngssTestSamplersDirid = Shader.PropertyToID("NGSS_TEST_SAMPLERS_DIR");
        private int _ngssFilterSamplersDirid = Shader.PropertyToID("NGSS_FILTER_SAMPLERS_DIR");
        private int _ngssGlobalSoftnessid = Shader.PropertyToID("NGSS_GLOBAL_SOFTNESS");
        private int _ngssGlobalSoftnessOptimizedid = Shader.PropertyToID("NGSS_GLOBAL_SOFTNESS_OPTIMIZED");
        private int _ngssOptimizedIterationsid = Shader.PropertyToID("NGSS_OPTIMIZED_ITERATIONS");
        private int _ngssOptimizedSamplersid = Shader.PropertyToID("NGSS_OPTIMIZED_SAMPLERS");
        private int _ngssNoiseToDitheringScaleDirid = Shader.PropertyToID("NGSS_NOISE_TO_DITHERING_SCALE_DIR");
        private int _ngssPcssFilterDirMinid = Shader.PropertyToID("NGSS_PCSS_FILTER_DIR_MIN");
        private int _ngssPcssFilterDirMaxid = Shader.PropertyToID("NGSS_PCSS_FILTER_DIR_MAX");
        private int _ngssCascadesSoftnessNormalizationid = Shader.PropertyToID("NGSS_CASCADES_SOFTNESS_NORMALIZATION");
        private int _ngssCascadesCountid = Shader.PropertyToID("NGSS_CASCADES_COUNT");
        private int _ngssCascadesSplitsid = Shader.PropertyToID("NGSS_CASCADES_SPLITS");
        private int _ngssCascadeBlendDistanceid = Shader.PropertyToID("NGSS_CASCADE_BLEND_DISTANCE");

        //DENOISER
        private int _denoiseKernelID = Shader.PropertyToID("DenoiseKernel");
        private int _denoiseSoftnessID = Shader.PropertyToID("DenoiseSoftness");
        private int _denoiseTextureID = Shader.PropertyToID("NGSS_DenoiseTexture");
        private int _denoiseEdgeToleranceID = Shader.PropertyToID("DenoiseEdgeTolerance");

        //public Texture noiseTexture;
        private CommandBuffer _computeDenoiseCB, _blendDenoiseCB;
        private bool _DENOISER_ENABLED;
        private int _DENOISER_PASSES = 1;

        private bool isSetup = false;
        private bool isInitialized = false;
        private bool isGraphicSet = false;
        private Light _DirLight;

        private Light DirLight
        {
            get
            {
                if (_DirLight == null)
                {
                    _DirLight = GetComponent<Light>();
                }

                return _DirLight;
            }
        }

        private Material _mMaterial;

        private Material mMaterial
        {
            set => _mMaterial = value;
            get
            {
                if (_mMaterial) return _mMaterial;
                if (denoiserShader == null)
                {
                    denoiserShader = Shader.Find("Hidden/NGSS_DenoiseShader");
                }

                _mMaterial = new Material(denoiserShader);
                if (_mMaterial) return _mMaterial;
                Debug.LogWarning("NGSS Warning: can't find NGSS_DenoiseShader shader, make sure it's on your project.", this);
                enabled = false;
                return _mMaterial;
            }
        }

        void OnDisable()
        {
            isInitialized = false;

            if (NGSS_KEEP_ONDISABLE)
                return;

            if (isGraphicSet)
            {
                isGraphicSet = false;
                GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, Shader.Find("Hidden/Internal-ScreenSpaceShadows"));
                GraphicsSettings.SetShaderMode(BuiltinShaderType.ScreenSpaceShadows, BuiltinShaderMode.UseBuiltin);
            }

            if (mMaterial)
            {
                DestroyImmediate(mMaterial);
                mMaterial = null;
            }

            RemoveCommandBuffer();
        }

        void RemoveCommandBuffer()
        {
            if (_computeDenoiseCB == null || _DirLight == null) return;

            foreach (var cmd in _DirLight.GetCommandBuffers(LightEvent.AfterScreenspaceMask))
            {
                if (cmd.name != _computeDenoiseCB.name) continue;
                _DirLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, cmd);
                break;
            }

            _computeDenoiseCB = null;

            foreach (var cmd in _DirLight.GetCommandBuffers(LightEvent.AfterScreenspaceMask))
            {
                if (cmd.name != _blendDenoiseCB.name) continue;
                _DirLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, cmd);
                break;
            }

            _blendDenoiseCB = null;
        }

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

        void Init()
        {
            if (isInitialized)
            {
                return;
            }

            if (isGraphicSet == false)
            {
                GraphicsSettings.SetShaderMode(BuiltinShaderType.ScreenSpaceShadows, BuiltinShaderMode.UseCustom);
                GraphicsSettings.SetCustomShader(BuiltinShaderType.ScreenSpaceShadows, Shader.Find("Hidden/NGSS_Directional")); //Shader.Find can sometimes return null in Player builds (careful).
                //DirLight.shadows = DirLight.shadows == LightShadows.None ? LightShadows.None : LightShadows.Soft;
                isGraphicSet = true;
            }

            if (NGSS_NOISE_TEXTURE == null)
            {
                NGSS_NOISE_TEXTURE = Resources.Load<Texture2D>("BlueNoise_R8_8");
            }

            Shader.SetGlobalTexture("_BlueNoiseTextureDir", NGSS_NOISE_TEXTURE);

            if (NGSS_DENOISER_ENABLED && !UnityEngine.XR.XRSettings.enabled) AddCommandBuffer();

            isInitialized = true;
        }

        void AddCommandBuffer()
        {
            if (_DirLight == null) return;

            _computeDenoiseCB = new CommandBuffer();
            _blendDenoiseCB = new CommandBuffer();

            _computeDenoiseCB.name = "NGSS_Directional Denoiser Computation";
            _blendDenoiseCB.name = "NGSS_Directional Denoiser Blending";

            var denoise1ID = Shader.PropertyToID("NGSS_DenoiseTexture1");
            var denoise2ID = Shader.PropertyToID("NGSS_DenoiseTexture2");

            _blendDenoiseCB.Blit(BuiltinRenderTextureType.None, BuiltinRenderTextureType.CurrentActive, mMaterial, 1);

            _computeDenoiseCB.GetTemporaryRT(denoise1ID, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.R8);
            _computeDenoiseCB.GetTemporaryRT(denoise2ID, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.R8);

            //_commandBuffer.SetGlobalTexture(denoiseID, BuiltinRenderTextureType.CurrentActive);

            for (var i = 1; i <= NGSS_DENOISER_PASSES; i++)
            {
                _computeDenoiseCB.SetGlobalVector(_denoiseKernelID, new Vector2(0f, 1f));
                if (i == 1)
                    _computeDenoiseCB.Blit(BuiltinRenderTextureType.CurrentActive, denoise2ID, mMaterial, 0);
                else
                    _computeDenoiseCB.Blit(denoise1ID, denoise2ID, mMaterial, 0);

                _computeDenoiseCB.SetGlobalVector(_denoiseKernelID, new Vector2(1f, 0f));
                _computeDenoiseCB.Blit(denoise2ID, denoise1ID, mMaterial, 0);
            }

            _computeDenoiseCB.SetGlobalTexture(_denoiseTextureID, denoise1ID);

            _computeDenoiseCB.ReleaseTemporaryRT(denoise1ID);
            _computeDenoiseCB.ReleaseTemporaryRT(denoise2ID);

            mMaterial.SetFloat(_denoiseEdgeToleranceID, NGSS_DENOISER_EDGE_SOFTNESS);
            mMaterial.SetFloat(_denoiseSoftnessID, NGSS_DENOISER_SOFTNESS);

            var canAdd = true;
            foreach (var cmd in _DirLight.GetCommandBuffers(LightEvent.AfterScreenspaceMask))
            {
                if (cmd.name != _computeDenoiseCB.name) continue;
                canAdd = false;
                break;
            }

            if (canAdd) _DirLight.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _computeDenoiseCB);

            canAdd = true;
            foreach (var cmd in _DirLight.GetCommandBuffers(LightEvent.AfterScreenspaceMask))
            {
                if (cmd.name != _blendDenoiseCB.name) continue;
                canAdd = false;
                break;
            }

            if (canAdd) _DirLight.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _blendDenoiseCB);
        }

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

        void Update()
        {
            //float dotAngle =  1f - Mathf.Abs((Vector3.Dot(Camera.main.transform.forward, Vector3.up) - 1f) / 2f);//0/1 range
            //float dotAngle = 1f - Mathf.Abs(Vector3.Dot(Camera.main.transform.forward, Vector3.up));-1/1 range
            //Debug.Log(dotAngle);
            //Shader.SetGlobalFloat("NGSS_DOT_ANGLE", dotAngle);

            if (Application.isPlaying && NGSS_NO_UPDATE_ON_PLAY && isSetup)
            {
                return;
            }

            if (DirLight.shadows == LightShadows.None || DirLight.type != LightType.Directional)
            {
                return;
            }

            //OBLIGATORY OR CREATES PROJECTION ISSUES IN SOME PLATFORMS
            //DirLight.shadows = LightShadows.Soft;

            //if (NGSS_BIAS_FADE) { Shader.EnableKeyword("NGSS_USE_BIAS_FADE_DIR"); Shader.SetGlobalFloat("NGSS_BIAS_FADE_DIR", NGSS_BIAS_FADE_VALUE * 0.001f); } else { Shader.DisableKeyword("NGSS_USE_BIAS_FADE_DIR"); }
            //if (NGSS_FORCE_HARD_SHADOWS) { Shader.EnableKeyword("NGSS_HARD_SHADOWS_DIR"); dirLight.shadows = LightShadows.Hard; return; } else { Shader.DisableKeyword("NGSS_HARD_SHADOWS_DIR"); dirLight.shadows = LightShadows.Soft; }

            Shader.SetGlobalFloat(_ngssDirSamplingDistanceid, NGSS_SAMPLING_DISTANCE);
            //Shader.SetGlobalFloat("NGSS_DIR_NOISE_DISTANCE", NGSS_NOISE_DISTANCE); 

            NGSS_SAMPLING_TEST = Mathf.Clamp(NGSS_SAMPLING_TEST, 4, NGSS_SAMPLING_FILTER);
            Shader.SetGlobalFloat(_ngssTestSamplersDirid, NGSS_SAMPLING_TEST);
            Shader.SetGlobalFloat(_ngssFilterSamplersDirid, NGSS_SAMPLING_FILTER);

            //Scale global softness over distance (to maintain the similar softness when texel size changes
            Shader.SetGlobalFloat(_ngssGlobalSoftnessid, QualitySettings.shadowProjection == ShadowProjection.CloseFit ? NGSS_SHADOWS_SOFTNESS : NGSS_SHADOWS_SOFTNESS * 2 / (QualitySettings.shadowDistance * 0.66f) * (QualitySettings.shadowCascades == 2 ? 1.5f : QualitySettings.shadowCascades == 4 ? 1f : 0.25f));

            //Directional OPTIMIZED
            Shader.SetGlobalFloat(_ngssGlobalSoftnessOptimizedid, NGSS_SHADOWS_SOFTNESS / (QualitySettings.shadowDistance)); //(256 / (GLOBAL_SOFTNESS / 20))        
            int optimizedSamplers = (int) Mathf.Sqrt(NGSS_SAMPLING_FILTER);
            Shader.SetGlobalInt(_ngssOptimizedIterationsid, optimizedSamplers % 2 == 0 ? optimizedSamplers + 1 : optimizedSamplers); //we need an odd number for Gaussian-Box filter
            Shader.SetGlobalInt(_ngssOptimizedSamplersid, NGSS_SAMPLING_FILTER);

            //DENOISER
            if (!UnityEngine.XR.XRSettings.enabled)
            {
                if (_DENOISER_ENABLED != NGSS_DENOISER_ENABLED)
                {
                    _DENOISER_ENABLED = NGSS_DENOISER_ENABLED;
                    RemoveCommandBuffer();
                    if (NGSS_DENOISER_ENABLED) AddCommandBuffer();
                }

                if (_DENOISER_PASSES != NGSS_DENOISER_PASSES)
                {
                    _DENOISER_PASSES = NGSS_DENOISER_PASSES;
                    RemoveCommandBuffer();
                    if (NGSS_DENOISER_ENABLED) AddCommandBuffer();
                }

                if (NGSS_DENOISER_ENABLED)
                {
                    mMaterial.SetFloat(_denoiseEdgeToleranceID, NGSS_DENOISER_EDGE_SOFTNESS);
                    mMaterial.SetFloat(_denoiseSoftnessID, NGSS_DENOISER_SOFTNESS);
                }
            }

            if (NGSS_RECEIVER_PLANE_BIAS)
            {
                Shader.EnableKeyword("NGSS_USE_RECEIVER_PLANE_BIAS"); /*Shader.SetGlobalFloat("NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR_DIR", NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR);*/
            }
            else
            {
                Shader.DisableKeyword("NGSS_USE_RECEIVER_PLANE_BIAS");
            }

            //Noise
            Shader.SetGlobalFloat(_ngssNoiseToDitheringScaleDirid, NGSS_NOISE_TO_DITHERING_SCALE);

#if !UNITY_5
            if (NGSS_PCSS_ENABLED)
            {
                float pcss_min = NGSS_PCSS_SOFTNESS_NEAR * 0.25f;
                float pcss_max = NGSS_PCSS_SOFTNESS_FAR * 0.25f;

                Shader.SetGlobalFloat(_ngssPcssFilterDirMinid, pcss_min > pcss_max ? pcss_max : pcss_min);
                Shader.SetGlobalFloat(_ngssPcssFilterDirMaxid, pcss_max < pcss_min ? pcss_min : pcss_max);

                Shader.EnableKeyword("NGSS_PCSS_FILTER_DIR");
            }
            else
            {
                Shader.DisableKeyword("NGSS_PCSS_FILTER_DIR");
            }
#endif

            if (NGSS_SHADOWS_RESOLUTION != ShadowMapResolution.UseQualitySettings)
            {
                DirLight.shadowCustomResolution = (int) NGSS_SHADOWS_RESOLUTION;
            }
            else
            {
                DirLight.shadowCustomResolution = 0;
                DirLight.shadowResolution = LightShadowResolution.FromQualitySettings;
            }

            /*
            GLOBAL_SHADOWS_DISTANCE = Mathf.Clamp(GLOBAL_SHADOWS_DISTANCE, 0f, GLOBAL_SHADOWS_DISTANCE);
            
            if (GLOBAL_SETTINGS_OVERRIDE)
            {
                QualitySettings.shadowDistance = GLOBAL_SHADOWS_DISTANCE;
                QualitySettings.shadowProjection = GLOBAL_SHADOWS_PROJECTION;
                
                GLOBAL_CASCADES_COUNT = GLOBAL_CASCADES_COUNT == 1 ? 0 : GLOBAL_CASCADES_COUNT == 3 ? 4 : GLOBAL_CASCADES_COUNT;
    
                if (GLOBAL_CASCADES_COUNT > 1)
                {
                    QualitySettings.shadowCascades = GLOBAL_CASCADES_COUNT;
                    QualitySettings.shadowCascade4Split = new Vector3(GLOBAL_CASCADES_SPLIT_VALUE, GLOBAL_CASCADES_SPLIT_VALUE * 2, GLOBAL_CASCADES_SPLIT_VALUE * 2 * 2);
                    QualitySettings.shadowCascade2Split = GLOBAL_CASCADES_SPLIT_VALUE * 2;
                }
                else
                    QualitySettings.shadowCascades = 0;
            }*/

            if (QualitySettings.shadowCascades > 1)
            {
                Shader.SetGlobalFloat(_ngssCascadesSoftnessNormalizationid, NGSS_CASCADES_SOFTNESS_NORMALIZATION);
                Shader.SetGlobalFloat(_ngssCascadesCountid, QualitySettings.shadowCascades);
                Shader.SetGlobalVector(_ngssCascadesSplitsid, QualitySettings.shadowCascades == 2 ? new Vector4(QualitySettings.shadowCascade2Split, 1f, 1f, 1f) : new Vector4(QualitySettings.shadowCascade4Split.x, QualitySettings.shadowCascade4Split.y, QualitySettings.shadowCascade4Split.z, 1f));
            }

            if (NGSS_CASCADES_BLENDING && QualitySettings.shadowCascades > 1)
            {
                Shader.EnableKeyword("NGSS_USE_CASCADE_BLENDING");
                Shader.SetGlobalFloat(_ngssCascadeBlendDistanceid, NGSS_CASCADES_BLENDING_VALUE * 0.125f);
            }
            else
            {
                Shader.DisableKeyword("NGSS_USE_CASCADE_BLENDING");
            }

            isSetup = true;
        }
    }
}