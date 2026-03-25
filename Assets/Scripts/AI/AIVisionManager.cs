using UnityEngine;
using Unity.Sentis;
using System.Collections;


public class AIVisionManager : MonoBehaviour
{
    private Worker worker;
    private Model runTimeModel;
    private Tensor<float> inputTensor;
    private Vector2 detectedScreenPos;

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
            float[] data = outputTensor.DownloadToArray();

            float maxConfidence = 0f;
            int bestAnchor = -1;
            int bestClass = -1;

            for (int col = 0; col < 8400; col++)
            {
                float class0Score = data[4 * 8400 + col];
                float class1Score = data[5 * 8400 + col];

                float conf0 = 1f / (1f + Mathf.Exp(-class0Score));
                float conf1 = 1f / (1f + Mathf.Exp(-class1Score));

                float currentMax = Mathf.Max(conf0, conf1);

                if (currentMax > maxConfidence)
                {
                    maxConfidence = currentMax;
                    bestAnchor = col;
                    bestClass = (conf0 > conf1) ? 0 : 1;
                }
            }

            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"<color=orange>[AI Debug] Highest Confidence found: {maxConfidence:P2}</color>");
            }

            if (maxConfidence > 0.7f && bestAnchor != -1)
            {
                float xCenter = data[0 * 8400 + bestAnchor];
                float yCenter = data[1 * 8400 + bestAnchor];

                detectedScreenPos = new Vector2(xCenter / 640f, 1f - (yCenter / 640f));

                if (bestClass == 0)
                {
                    Debug.Log($"<color=cyan>[AI] HEADPHONE CASE FOUND! ({maxConfidence:P0}) at {detectedScreenPos}</color>");
                    worldBridge.OnObjectDetected(detectedScreenPos);
                }
                else if (bestClass == 1)
                {
                    Debug.Log($"<color=cyan>[AI] WATER BOTTLE FOUND! ({maxConfidence:P0}) at {detectedScreenPos}</color>");
                    worldBridge.OnObjectDetected(detectedScreenPos);
                }
                else
                {
                    Debug.Log($"[AI] No valid objects detected ({maxConfidence:P0}), ignoring.");
                }
            }
        }
        else
        {
            Debug.LogError("[AI] Worker scheduled but output tensor is NULL!");
        }
    }
}
