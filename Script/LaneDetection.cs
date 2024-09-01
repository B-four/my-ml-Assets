using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class LaneDetection : MonoBehaviour
{
    public Camera frontCamera;
    public RenderTexture renderTexture;
    public Texture2D capturedImage;

    void Start()
    {
        // 카메라에서 이미지를 캡처할 RenderTexture 설정
        renderTexture = new RenderTexture(84, 84, 24);
        frontCamera.targetTexture = renderTexture;
        capturedImage = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
    }

    void Update()
    {
        // RenderTexture에서 이미지를 가져와 Texture2D로 변환
        RenderTexture.active = renderTexture;
        capturedImage.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        capturedImage.Apply();
        RenderTexture.active = null;

        // 차선 검출
        Vector2 laneCenter = DetectLane(capturedImage);
        LaneCenter = laneCenter;

        // 차선 검출 결과를 디버깅용으로 화면에 출력
        GetComponent<Renderer>().material.mainTexture = capturedImage;
    }

    public Vector2 LaneCenter { get; private set; }

    Vector2 DetectLane(Texture2D image)
    {
        int width = image.width;
        int height = image.height;
        Color[] pixels = image.GetPixels();

        int leftEdge = -1;
        int rightEdge = -1;

        int leftEdgeCount = 0;
        int rightEdgeCount = 0;
        int threshold = 10; // 점선 차선을 감지하기 위한 임계값

        // 이미지의 하단 절반에서 차선 가장자리 탐색
        for (int y = height / 2; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = pixels[y * width + x];
                float brightness = pixelColor.grayscale; // 흑백 변환

                if (brightness > 0.7f) // 밝은 픽셀(차선으로 가정)
                {
                    if (leftEdge == -1 || (x - leftEdge) > threshold)
                    {
                        leftEdge = x; // 왼쪽 가장자리
                        leftEdgeCount++;
                    }
                    else if (x > leftEdge + threshold)
                    {
                        rightEdge = x; // 오른쪽 가장자리
                        rightEdgeCount++;
                    }
                }
            }
        }

        // 좌우 가장자리가 일정한 패턴으로 감지된 경우 중심 계산
        if (leftEdgeCount > 1 && rightEdgeCount > 1)
        {
            float laneCenterX = (leftEdge + rightEdge) / 2.0f;
            return new Vector2(laneCenterX, height / 2);
        }

        // 차선을 찾지 못한 경우 중앙을 기본값으로 반환
        return new Vector2(width / 2, height / 2);
    }
}
