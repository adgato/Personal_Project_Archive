using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer tile;
    public enum Type { Input, Output, Transmit }

    private Type type;
    private bool state;
    public bool selected { get; private set; }

    private Node node;


    private void LateUpdate()
    {
        if (node != null && node.GetCachedOutput(0, out bool output))
            SetMode(output && node.OutPortCount < 2);

    }

    public void UpdateSelected(DragBounds selectionBox)
    {
        for (int i = 0; i < node.InPortCount; i++)
        {
            if (selectionBox.Contains(node.GetInputTile(i)))
            {
                selected = true;
                tile.color = new Color32(0x66, 0xFF, 0xFF, 0xFF);
                return;
            }
        }
        for (int i = 0; i < node.OutPortCount; i++)
        {
            if (selectionBox.Contains(node.GetOutputTile(i)))
            {
                selected = true;
                tile.color = new Color32(0x66, 0xFF, 0xFF, 0xFF);
                return;
            }
        }
        Unselect();
    }
    public void Unselect()
    {
        selected = false;
        SetMode(type, state);
    }
    public void MoveSelected(Vector2Int displacement)
    {
        if (selected)
            Move(node.GetCentre() + displacement);
    }

    public void SetNode(Node node)
    {
        if (this.node != null)
            Debug.LogWarning("Warning: Overwriting current node");
        this.node = node;
    }

    public void SetTileScale(Vector2Int scale) => tile.transform.localScale = new Vector3(scale.x, scale.y, 1);
    public void SetSprite(Sprite sprite) => tile.sprite = sprite;

    public void SetMode(Type type, bool state)
    {
        this.type = type;
        this.state = state;

        if (selected)
            return;

        tile.color = node != null && node.inCycle ? new Color32(0xFF, 0x00, 0x66, 0xFF) : type switch
        {
            Type.Input => state ? new Color32(0x00, 0x66, 0xFF, 0xFF) : new Color32(0x00, 0x66, 0x66, 0xFF),
            Type.Output => state ? new Color32(0xFF, 0x66, 0x00, 0xFF) : new Color32(0x66, 0x66, 0x00, 0xFF),
            Type.Transmit => state ? new Color32(0x66, 0xFF, 0x66, 0xFF) : new Color32(0x66, 0x66, 0x66, 0xFF),
            _ => Color.black
        };
    }
    public void SetMode(bool state) => SetMode(type, state);
    public bool Toggle()
    {
        state ^= true;
        SetMode(state);
        return state;
    }

    public void Move(Vector2Int coord)
    {
        node.Move(coord);
        transform.position = new Vector3(coord.x, coord.y, 0);
    }
    public void PointSync()
    {
        tile.flipX = node.reverse;
        tile.flipY = node.flip ^ node.vertical;
        tile.transform.eulerAngles = new Vector3(0, 0, node.vertical ? 90 : 0);
    }
    public void PointRight()
    {
        node.PointRight();
        PointSync();
    }
    public void PointLeft()
    {
        node.PointLeft();
        PointSync();
    }
    public void PointUp()
    {
        node.PointUp();
        PointSync();
    }
    public void PointDown()
    {
        node.PointDown();
        PointSync();
    }
    public void Flip()
    {
        node.Flip();
        PointSync();
    }
}
