using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct DragBounds
{
    int minX;
    int maxX;
    int minY;
    int maxY;
    public void SetBetween(Vector2Int a, Vector2Int b)
    {
        minX = Mathf.Min(a.x, b.x);
        maxX = Mathf.Max(a.x, b.x);
        minY = Mathf.Min(a.y, b.y);
        maxY = Mathf.Max(a.y, b.y);
    }
    public bool Contains(Vector2Int coord) => minX < coord.x && coord.x < maxX && minY < coord.y && coord.y < maxY;
}

/// <summary>
/// There should only be one of these per scene.
/// </summary>
public class NodeInstance : MonoBehaviour
{
    private CustomNode node;

    public Camera mainCamera;
    public Hotbarbar hotbarbar;
    [SerializeField] private Transform xBar;
    [SerializeField] private Transform yBar;
    private Transform holder;

    [SerializeField] private GameObject textprefab;
    [SerializeField] private GameObject tileprefab;
    [SerializeField] private Sprite bufferSprite;
    [SerializeField] private Sprite nandSprite;
    [SerializeField] private Sprite onoffSprite;
    [SerializeField] private Sprite repeaterSprite;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite rectSprite;

    [SerializeField] private SpriteRenderer dragBox;
    [SerializeField] private Transform dragTransform;
    private DragBounds dragBounds;
    private Vector2Int dragStart;
    private Vector2Int dragEnd;
    private Vector3 screenDragStart;

    public readonly Dictionary<KeyCode, System.Func<Node>> Hotbar = new Dictionary<KeyCode, System.Func<Node>>(11)
    {
        { KeyCode.Alpha1, () => new Wire() },
        { KeyCode.Alpha2, () => new JumpWire() },
        { KeyCode.Alpha3, () => new Nand() },
        { KeyCode.Alpha4, () => new Repeater() },
        { KeyCode.Alpha5, () => new OnInput() },
        { KeyCode.Alpha6, () => new OffInput() },
        { KeyCode.Alpha7, () => new HideOutput() },
        { KeyCode.Alpha8, () => null },
        { KeyCode.Alpha9, () => null },
        { KeyCode.Alpha0, () => null },
        { KeyCode.Return, () => new Wire() }
    };

    public bool adding { get; private set; } = false;
    Vector2Int addingDirection = Vector2Int.right;

    bool cursorMoveAllowed;

    public Vector2Int cursorCoord { get; private set; }
    public Vector2Int arrowKey;

    private Stack<(Node, Tile)> undoStack = new Stack<(Node, Tile)>();

    private Dictionary<Node, Tile> nodeTiles = new Dictionary<Node, Tile>();
    private Dictionary<Vector2Int, TextMesh> textTiles = new Dictionary<Vector2Int, TextMesh>();

    private List<Tile> ioTiles = new List<Tile>();
    private HashSet<Vector2Int> ioCoords = new HashSet<Vector2Int>();

    private Dictionary<Vector2Int, (PortFwdNode, Tile)> inputTileCoords = new Dictionary<Vector2Int, (PortFwdNode, Tile)>();

    private void Start()
    {
        Load("Counter Demo");
        CustomNodeSaver.GetNodeTypes();
    }

    void Update()
    {
        GetArrowInput();
        CheckDragScreen();


        if (adding || SearchBox.active || EditTitle.interacting || SaveButton.interacting)
            return;

        cursorCoord += arrowKey;
        if (Input.GetMouseButton(0))
            cursorCoord = GetMouseCoord();

        CheckDrag();

        xBar.position = new Vector3(cursorCoord.x, mainCamera.transform.position.y, 1);
        yBar.position = new Vector3(mainCamera.transform.position.x, cursorCoord.y, 1);

        CheckUndo();
        AddSymbol(Input.inputString, cursorCoord, false);
        CheckNewNode();

        if (GetSelectUp() && inputTileCoords.ContainsKey(cursorCoord) && !AnySelected())
            inputTileCoords[cursorCoord].Item1.input = inputTileCoords[cursorCoord].Item2.Toggle();

        Propagate();

        arrowKey = Vector2Int.zero;
    }

    void Propagate()
    {
        node.PrePropagate();
        for (int i = 0; i < node.OutPortCount; i++)
            ioTiles[i].SetMode(node.GetOutput(i, false));
    }

    public void Save(string nodeName)
    {
        node.externalData.Name = nodeName;
        CustomNodeSaver.SaveCustomNode(node);
    }

    public void Delete(string customNodeName)
    {
        string filename = Application.persistentDataPath + "/" + customNodeName + ".json";
        if (System.IO.File.Exists(filename))
            System.IO.File.Delete(filename);
        CustomNodeSaver.GetNodeTypes();
    }

    public void Load(string customNodeName)
    {

        holder = new GameObject("Holder").transform;
        holder.parent = transform;
        holder.SetSiblingIndex(0);
        if (transform.childCount > 1)
            Destroy(transform.GetChild(1).gameObject);

        undoStack.Clear();
        nodeTiles.Clear();
        textTiles.Clear();
        ioTiles.Clear();
        ioCoords.Clear();
        inputTileCoords.Clear();

        if (node != null && customNodeName != "AutoSave")
            Save("AutoSave");

        node = CustomNodeSaver.LoadCustomNode(customNodeName);
        if (node == null)
            node = new CustomNode();

        foreach (Node subnode in node.subnodes)
        {
            Tile tile = Instantiate(tileprefab, Vector3.zero, Quaternion.identity, holder).GetComponent<Tile>();
            tile.SetMode(Tile.Type.Transmit, false);

            tile.SetNode(subnode);
            SetSprite(subnode, tile);

            tile.Move(subnode.externalData.offsetTileOrigin);
            tile.PointSync();
            nodeTiles.Add(subnode, tile);
        }
        RefreshInOut();
        for (int i = 0; i < node.inPortSymbols.Length; i++)
            AddSymbol(node.inPortSymbols[i], node.inPortSymbolPositions[i], false);
        for (int i = 0; i < node.outPortSymbols.Length; i++)
            AddSymbol(node.outPortSymbols[i], node.outPortSymbolPositions[i], false);
    }

    public void CheckNewNode()
    {
        foreach (KeyCode AlphaN in Hotbar.Keys)
        {
            if (!Input.GetKeyDown(AlphaN))
                continue;
            Node subnode = Hotbar[AlphaN]();
            if (subnode == null)
                return;
            Tile tile = Instantiate(tileprefab, Vector3.zero, Quaternion.identity, holder).GetComponent<Tile>();
            tile.SetMode(Tile.Type.Transmit, false);
            tile.SetNode(subnode);
            SetSprite(subnode, tile);
            StartCoroutine(CreateNode(tile, subnode, AlphaN));
            if (AlphaN != KeyCode.Return)
            {
                Hotbar[KeyCode.Return] = Hotbar[AlphaN];
                hotbarbar.Select(AlphaN);
            }
            return;
        }
    }
    IEnumerator CreateNode(Tile tile, Node subnode, KeyCode AlphaN)
    {
        adding = true;

        int inputNo = 0;
        tile.Move(cursorCoord);

        void pointInAddingDir()
        {
            if (addingDirection == Vector2Int.left) tile.PointLeft();
            else if (addingDirection == Vector2Int.right) tile.PointRight();
            else if (addingDirection == Vector2Int.up) tile.PointUp();
            else if (addingDirection == Vector2Int.down) tile.PointDown();
        }
        pointInAddingDir();

        while (!Input.GetKeyUp(AlphaN))
        {
            if (subnode.InPortCount > 0 && Input.GetKeyDown(KeyCode.Tab))
                inputNo = (inputNo + 1) % subnode.InPortCount;

            if (Input.GetKey(KeyCode.LeftArrow)) addingDirection = Vector2Int.left;
            else if (Input.GetKey(KeyCode.RightArrow)) addingDirection = Vector2Int.right;
            else if (Input.GetKey(KeyCode.UpArrow)) addingDirection = Vector2Int.up;
            else if (Input.GetKey(KeyCode.DownArrow)) addingDirection = Vector2Int.down;

            pointInAddingDir();

            if (GetArrowKeyDown())
                tile.Flip();

            if (subnode.InPortCount > 0)
                tile.Move(cursorCoord + subnode.GetCentre() - subnode.GetInputTile(inputNo));

            yield return null;
        }

        if (node.TryAddSubnode(subnode))
        {
            if (subnode.InPortCount > 0)
                cursorCoord = subnode.GetOutputTile(0);
            nodeTiles.Add(subnode, tile);
            RefreshInOut();
            while (undoStack.TryPop(out (Node _, Tile tile) remove))
                Destroy(remove.tile.gameObject);
        }
        else
        {
            RefreshInOut();
            Destroy(tile.gameObject);
        }
        if (AlphaN != KeyCode.Return)
            hotbarbar.UnSelect(AlphaN);
        adding = false;
    }

    void CheckUndo()
    {
        if (Input.GetKeyDown(KeyCode.Equals) && undoStack.TryPop(out (Node node, Tile tile) readd) && node.TryAddSubnode(readd.node))
        {
            if (readd.node.InPortCount > 0)
                cursorCoord = readd.node.GetOutputTile(0);
            readd.tile.gameObject.SetActive(true);
            nodeTiles.Add(readd.node, readd.tile);
            RefreshInOut();
        }
        else if (Input.GetKeyDown(KeyCode.Minus) && node.TryPopSubnode(out Node removeNode))
        {
            nodeTiles[removeNode].gameObject.SetActive(false);
            undoStack.Push((removeNode, nodeTiles[removeNode]));
            nodeTiles.Remove(removeNode);
            RefreshInOut();
        }
    }

    public bool AnySelected()
    {
        foreach (Node removeNode in nodeTiles.Keys)
            if (nodeTiles[removeNode].selected)
                return true;
        return false;
    }
    void CheckDrag()
    {
        dragBox.color = GetSelect() ? new Color32(0x66, 0xFF, 0xFF, 0x0D) : new Color32(0xFF, 0x22, 0x33, 0x7F);
        if (GetSelectDown())
        {
            dragBox.enabled = true;
            dragTransform.localScale = new Vector3(0, 0, 1);
            dragStart = cursorCoord;
            dragEnd = cursorCoord;
            dragTransform.position = new Vector3(dragStart.x, dragStart.y, -2);
            return;
        }
        else if (GetSelect())
        {
            dragEnd = cursorCoord;
            dragBounds.SetBetween(dragStart, dragEnd);
            dragTransform.localScale = new Vector3(dragEnd.x - dragStart.x, dragEnd.y - dragStart.y, 1);
            foreach (Node node in nodeTiles.Keys)
                nodeTiles[node].UpdateSelected(dragBounds);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.Delete))
        {
            dragBox.enabled = false;
            HashSet<Node> nodesToRemove = new HashSet<Node>();
            foreach (Node removeNode in nodeTiles.Keys)
            {
                if (!nodeTiles[removeNode].selected)
                    continue;
                nodeTiles[removeNode].Unselect();
                nodesToRemove.Add(removeNode);
                nodeTiles[removeNode].gameObject.SetActive(false);
                undoStack.Push((removeNode, nodeTiles[removeNode]));
            }
            foreach (Node removeNode in nodesToRemove)
                nodeTiles.Remove(removeNode);
            node.BatchRemoveSubnodes(nodesToRemove);
            RefreshInOut();
            return;
        }

        Vector2Int dragDelta = cursorCoord - dragEnd;
        dragEnd = cursorCoord;
        bool anySelected = AnySelected();
        dragBox.enabled = anySelected;

        if (!anySelected || !(dragDelta.x != 0 || dragDelta.y != 0))
            return;

        foreach (Node node in nodeTiles.Keys)
            nodeTiles[node].MoveSelected(dragDelta);

        if (node.TryLinkCircuit())
        {
            dragStart += dragDelta;
            dragTransform.position = new Vector3(dragStart.x, dragStart.y, -2);
        }
        else
        {
            //Undo the move
            cursorCoord -= dragDelta;
            dragEnd = cursorCoord;
            foreach (Node node in nodeTiles.Keys)
                nodeTiles[node].MoveSelected(-dragDelta);
            if (!node.TryLinkCircuit())
                Debug.LogError("Error: wut");
        }
        RefreshInOut();
    }
    void CheckDragScreen()
    {
        if (Input.GetMouseButtonDown(2))
            screenDragStart = GetMousePos() - mainCamera.transform.position;
        else if (Input.GetMouseButton(2))
        {
            Vector3 mousePos = GetMousePos() - mainCamera.transform.position;
            Vector3 screenDragDelta = mousePos - screenDragStart;
            screenDragStart = mousePos;
            mainCamera.transform.position -= screenDragDelta;
        }
        float cameraSize = Mathf.Clamp(mainCamera.orthographicSize - Input.GetAxis("Mouse ScrollWheel"), 4, 50);
        yBar.localScale = new Vector3(10 * cameraSize, 0.004f * cameraSize, 1);
        xBar.localScale = new Vector3(0.004f * cameraSize, 10 * cameraSize, 1);
        mainCamera.orthographicSize = cameraSize;
    }

    void RefreshInOut()
    {
        while (ioTiles.Count > node.InPortCount + node.OutPortCount)
        {
            GameObject tileObj = ioTiles[0].gameObject;
            ioTiles.RemoveAt(0);
            Destroy(tileObj);
        }
        while (ioTiles.Count < node.InPortCount + node.OutPortCount)
        {
            Transform tileTransform = Instantiate(tileprefab, Vector3.zero, Quaternion.identity, holder).transform;
            tileTransform.localScale = 0.9f * Vector3.one;
            ioTiles.Add(tileTransform.GetComponent<Tile>());
        }

        inputTileCoords.Clear();
        ioCoords.Clear();

        int c = 0;

        for (int i = 0; i < node.OutPortCount; i++)
        {
            Node.Socket socket = node.out2subout[i];
            Vector2Int coord = socket.GetTile();
            ioTiles[c].transform.position = new Vector3(coord.x, coord.y, -0.5f);
            ioTiles[c].SetMode(Tile.Type.Output, false);
            ioCoords.Add(coord);
            c++;
        }
        foreach (Vector2Int coord in node.portFwdNodes.Keys)
        {
            inputTileCoords.Add(coord, (node.portFwdNodes[coord], ioTiles[c]));
            ioTiles[c].transform.position = new Vector3(coord.x, coord.y, -0.5f);
            ioTiles[c].SetMode(Tile.Type.Input, false);
            ioCoords.Add(coord);
            c++;
        }

        List<Vector2Int> posToRemove = new List<Vector2Int>();
        foreach (Vector2Int pos in textTiles.Keys)
        {
            if (ioCoords.Contains(pos))
                continue;
            posToRemove.Add(pos);
            node.portSymbols.Remove(pos);
            Destroy(textTiles[pos].gameObject);
        }
        foreach (Vector2Int pos in posToRemove)
            textTiles.Remove(pos);
        foreach (Node node in nodeTiles.Keys)
        {
            for (int i = 0; i < node.inPortSymbols.Length; i++)
                AddSymbol(node.inPortSymbols[i], node.GetInputTile(i), true);
            for (int i = 0; i < node.outPortSymbols.Length; i++)
                AddSymbol(node.outPortSymbols[i], node.GetOutputTile(i), true);
        }
    }

    void AddSymbol(string inputstring, Vector2Int pos, bool force)
    {
        if (inputstring == null || inputstring.Length == 0)
            return;

        if (force)
        {
            if (!textTiles.ContainsKey(pos))
                textTiles[pos] = Instantiate(textprefab, new Vector3(pos.x, pos.y, -1), Quaternion.identity, holder).GetComponent<TextMesh>();
            textTiles[pos].text = node.portSymbols[pos] = inputstring;
            return;
        }

        const string allowedStrings = "ABCDEFGHIJKLMNOPQRSTUVWXYZ\b";
        inputstring = inputstring.ToUpper();
        string symbol = "";
        foreach (char sym in inputstring)
        {
            if (allowedStrings.Contains(sym))
                symbol += sym;
            if (symbol.Length > 2)
                break;
        }
        if (symbol.Length == 0 || !ioCoords.Contains(pos))
            return;

        if (!node.portSymbols.ContainsKey(pos))
            node.portSymbols[pos] = "";
        if (symbol.Contains("\b"))
            node.portSymbols[pos] = "";
        else if (symbol != "")
            node.portSymbols[pos] += symbol;
        if (node.portSymbols[pos].Length > 2)
            node.portSymbols[pos] = node.portSymbols[pos][^2..];
        if (!textTiles.ContainsKey(pos))
            textTiles[pos] = Instantiate(textprefab, new Vector3(pos.x, pos.y, -1), Quaternion.identity, holder).GetComponent<TextMesh>();
        textTiles[pos].text = node.portSymbols[pos];
    }

    private void SetSprite(Node node, Tile tile)
    {
        System.Type t = node.GetType();
        if (t == typeof(Wire)) tile.SetSprite(bufferSprite);
        else if (t == typeof(JumpWire)) tile.SetSprite(jumpSprite);
        else if (t == typeof(Nand)) tile.SetSprite(nandSprite);
        else if (t == typeof(OnInput) || t == typeof(OffInput) || t == typeof(HideOutput)) tile.SetSprite(onoffSprite);
        else if (t == typeof(Repeater)) tile.SetSprite(repeaterSprite);
        else if (t == typeof(CustomNode))
        {
            CustomNode customNode = (CustomNode)node;
            tile.SetTileScale(new Vector2Int(customNode.localTileWidth, customNode.localTileHeight));
            tile.SetSprite(rectSprite);
        }
    }

    public void BindNode(KeyCode AlphaN, string nodeName)
    {
        Hotbar[AlphaN] = () => CustomNodeSaver.NewNode(nodeName);
        if (AlphaN != KeyCode.Return)
            hotbarbar.Bind(AlphaN, nodeName);
    }


    bool GetSelect() => Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
    bool GetSelectDown() => Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.Space) || !Input.GetMouseButton(0) && Input.GetKeyDown(KeyCode.Space);
    bool GetSelectUp() => Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.Space) || !Input.GetMouseButton(0) && Input.GetKeyUp(KeyCode.Space);
    bool GetArrowKeyDown() => Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow);
    Vector2Int GetMouseCoord()
    {
        Vector3 mousePos = GetMousePos();
        return new Vector2Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));
    }
    Vector3 GetMousePos() => mainCamera.ScreenToWorldPoint(Input.mousePosition);

    private void GetArrowInput()
    {
        if (arrowKey != Vector2Int.zero)
            return;

        if (GetArrowKeyDown())
            cursorMoveAllowed = !adding;

        if (!cursorMoveAllowed)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            arrowKey = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            arrowKey = Vector2Int.right;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            arrowKey = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            arrowKey = Vector2Int.down;
        else
            arrowKey = Vector2Int.zero;
    }

}
