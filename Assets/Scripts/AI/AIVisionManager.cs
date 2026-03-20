using UnityEngine;
using Unity.Sentis;
using System.Collections;


public class AIVisionManager : MonoBehaviour
{
    private Worker worker;
    private Model runTimeModel;
    private Tensor<float> inputTensor;

    public ModelAsset yoloModel;
    public DetectionWorldBridge worldBridge;

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            runTimeModel = ModelLoader.Load(yoloModel);
            worker = new Worker(runTimeModel, BackendType.GPUCompute);
            inputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));
            Debug.Log("<color=green>[AI] SUCCESS: Sentis 2.1+ Engine Initialized on GPU.</color>");
        }
        catch (System.Exception e) 
        { 
            Debug.LogError($"<color=red>[AI] CRITICAL ERROR during init:</color> {e.Message}"); 
        } 
        
    }

    public void DetectObjects(Texture frame)
    {
        TextureConverter.ToTensor(frame, inputTensor, new TextureTransform());
        worker.Schedule(inputTensor);

        var outputTensor = worker.PeekOutput() as Tensor<float>;

        if (outputTensor != null)
        {
            worldBridge.OnObjectDetected(new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogError("[AI] Worker scheduled but output tensor is NULL!");
        }
    }

    private void OnDestroy()
    {
        worker?.Dispose();
        inputTensor?.Dispose();
    }
}
