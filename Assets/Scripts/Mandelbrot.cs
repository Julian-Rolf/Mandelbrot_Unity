using UnityEngine;

public class Mandelbrot : MonoBehaviour
{
    private const int ITERATIONS = 256;

    private ComputeBuffer x;
    private ComputeBuffer y;
    private ComputeBuffer colorMapBuffer;
    private RenderTexture result;

    public Color[] ColorMap;
    [SerializeField]
    private float leftRight = 0;
    [SerializeField]
    private float upDown = 0;
    [SerializeField]
    private float zoom = 1;

    [SerializeField]
    private ComputeShader shader;

    private int width;
    private int height;

    private int kernelId;

    private float[] xValues;
    private float[] yValues;

    private Camera Camera;

    private void Start()
    {
        Application.targetFrameRate = 144;
        CreateColorMap(ITERATIONS);
    }

    private void CreateColorMap(int colorCount)
    {
        ColorMap = new Color[colorCount];
        int third = Mathf.CeilToInt(colorCount / 3f);
        float stepSize = 1f / third;

        for (int i = 0; i < third; i++)
        {
            ColorMap[i] = new Color(i * stepSize,0f,0f, 1f);
        }

        for (int i = 0; i < third; i++)
        {
            ColorMap[i + third] = new Color(1f - i * stepSize, stepSize * i, 0f, 1f);
        }

        for (int i = 0; i < third; i++)
        {
            if (i + third * 2 >= colorCount) break;

            ColorMap[i + third * 2] = new Color(0f, 1f - i * stepSize, stepSize * i, 1f);
        }
    }

    private void SetupGPU()
    {
        width = Screen.width;
        height = Screen.height;

        x?.Dispose();
        y?.Dispose();
        colorMapBuffer?.Dispose();

        x = new ComputeBuffer(width, 4);
        y = new ComputeBuffer(height, 4);
        colorMapBuffer = new ComputeBuffer(ITERATIONS, 4 * 4);

        xValues = Linspace(-2f / zoom + leftRight, 2f / zoom + leftRight, width);
        yValues = Linspace(-2f / zoom + upDown, 2f  / zoom + upDown, height);

        x.SetData(xValues);
        y.SetData(yValues);
        colorMapBuffer.SetData(ColorMap);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        SetupGPU();
        RunKernel(dest);
    }

    private void RunKernel(RenderTexture texture)
    {
        kernelId = shader.FindKernel("mandel");
        if(result != null) result.Release();
        result = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) {enableRandomWrite = true};
        result.Create();

        shader.SetTexture(kernelId, "img", result);
        shader.SetBuffer(kernelId, "xcoord", x);
        shader.SetBuffer(kernelId, "ycoord", y);
        shader.SetBuffer(kernelId, "colormap", colorMapBuffer);
        shader.SetInt("height", height);
        shader.SetInt("width", width);
        shader.SetInt("iterations", ITERATIONS);

        int xGroup = Mathf.CeilToInt((float) width / 8);
        int yGroup = Mathf.CeilToInt((float) height / 8);

        shader.Dispatch(kernelId, xGroup, yGroup,1);
        Graphics.Blit(result, texture);
    }

    private float[] Linspace(float start, float end, int number)
    {
        float stepSize = Mathf.Abs(start - end) / (number - 1);
        float[] points = new float[number];
        float lastValue = start;

        for (int i = 0; i < number - 1; i++)
        {
            points[i] = lastValue;
            lastValue += stepSize;
        }

        points[number - 1] = end;
        return points;
    }

    private void OnDestroy()
    {
        x.Release();
        y.Release();
        result.Release();
        colorMapBuffer.Release();

        x = null;
        y = null;
        result = null;
    }
}
