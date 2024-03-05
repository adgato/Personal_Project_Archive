using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RisingNote : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private float cutoffHeight;
    [SerializeField] private float riseSpeed;
    private float offset;
    private float height = 0;
    private bool rising = false;

    // Start is called before the first frame update
    public void Init(float initialVerticalOffset)
    {
        offset = initialVerticalOffset;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, offset);
    }

    // Update is called once per frame
    void Update()
    {
        if (!rising)
        {
            height += Time.smoothDeltaTime * riseSpeed;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
        else
        {
            offset += Time.smoothDeltaTime * riseSpeed;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, offset);
        }
        if (offset > cutoffHeight)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    public void Rise() => rising = true;
}
