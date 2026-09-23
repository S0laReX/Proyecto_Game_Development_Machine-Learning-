using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(100)]
public class CellPerformanceProbe : MonoBehaviour
{
    public CellSpawner spawner;
    [Min(1f)] public float warmupSeconds = 5f;
    [Min(1f)] public float sampleSeconds = 50f;
    [Tooltip("Solo para el ejecutable de medición automática.")]
    public bool quitWhenComplete;
    public bool runIntegrationChecks;
    private float warmup, elapsed, worstFrame;
    private int frames, slowFrames, wrongPopulationFrames;
    private int renderedFrames, lastRenderedFrame = -1;
    private bool completed;

    private void Start()
    {
        RenderPipelineManager.endCameraRendering += OnCameraRendered;
        if (quitWhenComplete)
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 120;
        }
        if (!runIntegrationChecks) return;
        try { CellRuntimeChecks.Run(spawner); }
        catch (System.Exception error)
        {
            completed = true;
            Debug.LogException(error, this);
            if (quitWhenComplete) Application.Quit(2);
        }
    }

    private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
    {
        if (frames == 0 || completed || lastRenderedFrame == Time.frameCount) return;
        lastRenderedFrame = Time.frameCount;
        renderedFrames++;
    }

    private void OnDestroy() => RenderPipelineManager.endCameraRendering -= OnCameraRendered;

    private void LateUpdate()
    {
        if (completed || spawner == null) return;
        float dt = Time.unscaledDeltaTime;
        warmup += dt;
        if (warmup < warmupSeconds) return;
        elapsed += dt;
        frames++;
        if (dt > worstFrame) worstFrame = dt;
        if (dt > 1f / 30f) slowFrames++;
        if (spawner.ActiveCount != 100) wrongPopulationFrames++;
        if (elapsed < sampleSeconds) return;
        completed = true;
        // LateUpdate precede al renderizado del último frame de la muestra.
        bool passed = slowFrames == 0 && wrongPopulationFrames == 0 && renderedFrames >= frames - 1;
        Debug.Log($"Rendimiento {(passed ? "PASS" : "FAIL")}: FPS medio={frames / elapsed:F1}, " +
            $"FPS mínimo={1f / worstFrame:F1}, frames >33.33ms={slowFrames}/{frames}, " +
            $"frames sin 100 células={wrongPopulationFrames}, frames renderizados={renderedFrames}. " +
            $"Resolución={Screen.width}x{Screen.height}, dispositivo={SystemInfo.graphicsDeviceName}", this);
        if (quitWhenComplete) Application.Quit(passed ? 0 : 1);
    }
}
