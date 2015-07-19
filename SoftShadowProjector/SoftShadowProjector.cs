using System;
using UnityEngine;

namespace RainyDays
{
	/// <summary>
	/// Creates a soft shadow for the host object, and projects it on a plane.
	/// </summary>
	public class SoftShadowProjector : MonoBehaviour
	{
		public Light lightSource;
		public GameObject receiver;
		public Color shadowColor = Color.gray;
		public int shadowTextureSize = 256;

		[Range(0.0f, 1.0f)]
		public float blurStep = 0.4f;
		[Range(0.0f, 1.0f)]
		public float blurSlope = 1.0f;

		private GameObject projectorGo;
		private Material projectorMaterial;
		private RenderTexture shadowRT;

		private static int kProjectorLayer, kCreateProjectorLayer;

		public static void ConfigureLayers()
		{
			kProjectorLayer = Layers.GetOrCreateByName("RainyDays-SoftShadowProjector");
			kCreateProjectorLayer = Layers.GetOrCreateByName(Layers.IsolateLayerName);
		}

		private static readonly float[][] KawaseKernels = new float[][]
		{
			new float[] { 0, 0 }, // 7 - 64
			new float[] { 0, 1, 1 }, // 15 - 128
			new float[] { 0, 1, 1, 2 }, // 23 - 256
			new float[] { 0, 1, 2, 2, 3 }, // 35 - 512
			new float[] { 0, 1, 2, 3, 4, 4, 5 }, // 63 - 1024
			new float[] { 0, 1, 2, 3, 4, 5, 7, 8, 9, 10 } // 127 - 2048
		};
		private static readonly float[] KawaseKernelSizes = new float[] { 7, 15, 23, 35, 63, 127 };

		private static float[] SelectKawaseKernel(int rtSize, out int numIterations, out float kernelSize)
		{
			rtSize = Mathf.NextPowerOfTwo(rtSize);
			int kernelIndex = 0;
			const int kMinExpectedSize = 64;
			while (rtSize > kMinExpectedSize)
			{
				++kernelIndex;
				rtSize >>= 1;
			}
			numIterations = 1;
			if (kernelIndex >= KawaseKernels.Length)
			{
				numIterations = kernelIndex - KawaseKernels.Length + 1;
				kernelIndex = KawaseKernels.Length - 1;
			}
			var kernel = KawaseKernels[kernelIndex];
			kernelSize = numIterations * KawaseKernelSizes[kernelIndex];
			return kernel;
		}

		/// <summary>
		/// Masaki Kawase in his GDC2003 presentation “Frame Buffer Postprocessing Effects in DOUBLE-S.T.E.A.L (Wreckless)”
		/// See: http://www.daionet.gr.jp/~masa/archives/GDC2003_DSTEAL.ppt
		/// See also: https://software.intel.com/en-us/blogs/2014/07/15/an-investigation-of-fast-real-time-gpu-based-image-blur-algorithms
		/// </summary>
		private static void KawaseBlur(RenderTexture rt, float[] kernel, int blurIterations)
		{
			var copyMat = new Material(Shader.Find("Hidden/RainyDays/ShadowCopy"));
			var blurMat = new Material(Shader.Find("Hidden/RainyDays/ShadowBlur"));

			const int downsample = 1;
			var src = RenderTexture.GetTemporary(rt.width >> downsample, rt.height >> downsample, 0, rt.format);
			src.filterMode = FilterMode.Bilinear;
			src.wrapMode = TextureWrapMode.Clamp;
			var dst = RenderTexture.GetTemporary(src.width, src.height, src.depth, src.format);
			dst.filterMode = FilterMode.Bilinear;
			dst.wrapMode = TextureWrapMode.Clamp;

			// downsample
			Graphics.Blit(rt, src, copyMat);

			// blur
			var passes = kernel.Length;
			var spreadId = Shader.PropertyToID("_Spread");
			for (int b = 0; b < blurIterations; ++b)
			{
				for (int i = 0; i < passes; ++i)
				{
					blurMat.SetFloat(spreadId, kernel[i]);
					Graphics.Blit(src, dst, blurMat);
					var t = src;
					src = dst;
					dst = t;
				}
			}

			// upsample
			Graphics.Blit(src, rt, copyMat);

			RenderTexture.ReleaseTemporary(src);
			RenderTexture.ReleaseTemporary(dst);
		}

		public byte[] ExportShadowPNG()
		{
			if (!shadowRT)
			{
				return null;
			}

			RenderTexture.active = shadowRT;
			var shadowTex = new Texture2D(shadowRT.width, shadowRT.height, TextureFormat.ARGB32, false);
			shadowTex.ReadPixels(new Rect(0.0f, 0.0f, (float)shadowRT.width, (float)shadowRT.height), 0, 0);
			shadowTex.Apply();
			var pngBytes = shadowTex.EncodeToPNG();
			return pngBytes;
		}

		public bool SaveShadowPNG(string pngFilePath)
		{
			var bytes = ExportShadowPNG();
			if (bytes == null)
			{
				return false;
			}
			try
			{
				System.IO.File.WriteAllBytes(pngFilePath, bytes);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex, this);
				return false;
			}
		}

		void Start()
		{
			ConfigureLayers();

			if (kProjectorLayer == -1 || kCreateProjectorLayer == -1)
			{
				return;
			}

			var startTime = DateTime.Now;
			if (!lightSource || !receiver)
			{
				Debug.LogError("Light Source or Receiver not set", this);
				return;
			}
			if (lightSource.type != LightType.Directional)
			{
				Debug.LogError("Only directional light sources supported. This light is of type: " + lightSource.type.ToString(), this);
				return;
			}

			shadowTextureSize = Math.Max(64, Mathf.NextPowerOfTwo(shadowTextureSize));

			if (lightSource.shadows != LightShadows.None)
			{
				// disable shadows on the source light since we're using projectors to replace them
				lightSource.shadows = LightShadows.None;
			}

			var shadowCaster = this.gameObject;
			var renderers = shadowCaster.GetComponentsInChildren<Renderer>();
			if (renderers.Length == 0)
			{
				Debug.LogError("No active renderer found on shadow caster", this);
				return;
			}
			var bounds = renderers[0].bounds;
			for (int i = 1; i < renderers.Length; ++i)
			{
				bounds.Encapsulate(renderers[i].bounds);
			}
			int[] oldLayers = new int[renderers.Length];
			int maskCreateProjector = 1 << kCreateProjectorLayer;
			for (int i = 0; i < renderers.Length; ++i)
			{
				oldLayers[i] = renderers[i].gameObject.layer;
				renderers[i].gameObject.layer = kCreateProjectorLayer;
			}

			// create a projector GO and attach it to the light
			projectorGo = new GameObject(shadowCaster.name + "-Projector");
			var projectorTransform = projectorGo.transform;
			projectorTransform.SetParent(lightSource.transform, false);

			// project bounds onto light XY plane
			var localBounds = BoundsMath.InverseTransform(bounds, projectorTransform);
			var projCenter = localBounds.center;
			projCenter.z = 0.0f;
			var projSize = localBounds.size;
			projSize.z = 0.0f;
			var orthoSize = Mathf.Max(projSize.x, projSize.y);
			orthoSize *= 1.25f; // to allow some space for blurred shadows - todo: configurable? dynamic?
			var orthoHalfSize = orthoSize * 0.5f;
			projSize.x = projSize.y = orthoSize;
			var projBounds = new Bounds(projCenter, projSize);

			// move the projector in front of the object
			float nearPlane = 0.1f;
			float zOffset = BoundsMath.MinDistanceFromPlane(bounds, projectorTransform.forward, projectorTransform.position) - nearPlane;
			Vector3 centerOffset = projBounds.center + new Vector3(0f, 0f, zOffset);
			projectorTransform.localPosition += centerOffset;

			// the far plane is the maximum projected distance of the AABB on the receiver plane (approximation)
			int blurIterations;
			float blurKernelSize;
			float[] blurKernel = SelectKawaseKernel(shadowTextureSize, out blurIterations, out blurKernelSize);
			var receiverTransform = receiver.transform;
			float farPlane = BoundsMath.MaxProjectedDistanceFromPlane(bounds, receiverTransform.up, receiverTransform.position, projectorTransform.forward);
			farPlane *= 1.0f + (blurKernelSize * 0.5f / (float)shadowTextureSize); // increase far plane to take blur into account (approximation)
			farPlane += nearPlane;

			// add a temp camera to render the shadow texture
			var shadowCam = projectorGo.AddComponent<Camera>();
			shadowCam.orthographic = true;
			shadowCam.orthographicSize = orthoHalfSize;
			shadowCam.nearClipPlane = nearPlane;
			shadowCam.farClipPlane = farPlane;
			shadowCam.renderingPath = RenderingPath.Forward;
			shadowCam.hdr = false;
			shadowCam.useOcclusionCulling = false;
			shadowCam.clearFlags = CameraClearFlags.SolidColor;
			shadowCam.backgroundColor = new Color(1f, 1f, 1f, 0f);
			// setup render to texture
			shadowRT = shadowCam.targetTexture = new RenderTexture(shadowTextureSize, shadowTextureSize, 24, RenderTextureFormat.ARGB32);
			shadowRT.filterMode = FilterMode.Bilinear;
			shadowRT.wrapMode = TextureWrapMode.Clamp;
			shadowRT.generateMips = false;
			shadowRT.Create();
			// only render caster objects
			shadowCam.cullingMask = maskCreateProjector;

			shadowCam.RenderWithShader(Shader.Find("Hidden/RainyDays/ShadowReplacement"), null);

			// blur
			var blurStart = DateTime.Now;
			KawaseBlur(shadowRT, blurKernel, blurIterations);
			var blurEnd = DateTime.Now;

			// draw 1px border to avoid clamping artifacts
			{
				RenderTexture.active = shadowRT;
				GL.PushMatrix();
				var borderMat = new Material(Shader.Find("Hidden/RainyDays/ShadowBorder"));
				borderMat.SetPass(0); // bind shader to use vertex color
				float w = (float)shadowRT.width;
				float h = (float)shadowRT.height;
				float c = 0.5f; // pixel center
				GL.LoadPixelMatrix(0f, w, 0f, h);
				GL.Viewport(new Rect(0f, 0f, w, h));
				GL.Begin(GL.LINES); // always 1px wide on target
				GL.Color(Color.white);
				// bottom
				GL.Vertex3(0f, c, 0f);
				GL.Vertex3(w, c, 0f);
				// right
				GL.Vertex3(w - c, 0f, 0f);
				GL.Vertex3(w - c, h, 0f);
				// top
				GL.Vertex3(w, h - c, 0f);
				GL.Vertex3(0f, h - c, 0f);
				// left
				GL.Vertex3(c, h, 0f);
				GL.Vertex3(c, 0f, 0f);
				GL.End();
				GL.PopMatrix();
			}

			int maskProjector = 1 << kProjectorLayer;

			// setup projector component
			var projector = projectorGo.AddComponent<Projector>();
			projector.orthographic = true;
			projector.orthographicSize = orthoHalfSize;
			projector.nearClipPlane = nearPlane;
			projector.farClipPlane = farPlane;
			projectorMaterial = projector.material = new Material(Shader.Find("Hidden/RainyDays/ShadowProjector"));
			projectorMaterial.SetTexture("_ShadowTex", shadowRT);
			UpdateProjectorMaterial();
			projector.ignoreLayers = maskProjector;

			// set layers to Projector
			for (int i = 0; i < renderers.Length; ++i)
			{
				renderers[i].gameObject.layer = kProjectorLayer;
			}

			// set the projector on the caster
			projectorTransform.SetParent(this.transform, true);

			// destroy camera
			RenderTexture.active = null;
			shadowCam.targetTexture = null;
			shadowCam.enabled = false;
			//Destroy(shadowCam);

			Debug.Log("Created projector in " + (DateTime.Now - startTime).TotalMilliseconds.ToString("0.00") + " ms (blur = " + (blurEnd - blurStart).TotalMilliseconds.ToString("0.00") + " ms)", this);
		}

		void Update()
		{
			if (projectorMaterial)
			{
				UpdateProjectorMaterial();
			}
		}

		private void UpdateProjectorMaterial()
		{
			projectorMaterial.SetColor("_ShadowTint", shadowColor);
			projectorMaterial.SetVector("_ShadowFactors", new Vector4(blurStep, blurSlope, 0f, 0f));
		}

		void OnDestroy()
		{
			if (projectorGo)
			{
				Destroy(projectorGo);
			}
		}
	}
}
