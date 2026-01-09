using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class Board
{
    public enum eMatchDirection
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL
    }

    private int boardSizeX;

    private int boardSizeY;

    private Cell[,] m_cells;

    public Cell[,] m_checkRow;

    private Transform m_root;

    private int m_matchMin;

    public Board(Transform transform, GameSettings gameSettings)
    {
        m_root = transform;

        m_matchMin = gameSettings.MatchesMin;

        this.boardSizeX = gameSettings.BoardSizeX;
        this.boardSizeY = gameSettings.BoardSizeY;

        m_cells = new Cell[boardSizeX, boardSizeY];
        m_checkRow = new Cell[5, 2];

        CreateBoard();
        CreateCheckRow();

    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(m_root);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);

                m_cells[x, y] = cell;
            }
        }

        //set neighbours
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
                if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
            }
        }

    }

    private void CreateCheckRow()
    {
        UnityEngine.Debug.Log("Create Check Row");
        Vector3 origin = new Vector3(-boardSizeX * 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        float offsetY = boardSizeY - 7.5f;
        for (int x = 0; x < 5; x++)
        {
            GameObject go = GameObject.Instantiate(prefabBG);
            go.transform.position = origin + new Vector3(x, offsetY, 0f);
            go.transform.SetParent(m_root);

            Cell cell = go.GetComponent<Cell>();
            cell.Setup(x, 1);

            m_checkRow[x, 1] = cell;
        }

    }

    internal void Fill()
    {
        // Tính tổng số cells
        int totalCells = boardSizeX * boardSizeY;

        // Lấy tất cả loại item
        NormalItem.eNormalType[] allTypes = System.Enum.GetValues(typeof(NormalItem.eNormalType)) as NormalItem.eNormalType[];
        int typeCount = allTypes.Length;

        // Chia items sao cho mỗi loại có số lượng chia hết cho 3
        int itemsPerType = (totalCells / typeCount / 3) * 3;
        int remainingCells = totalCells - (itemsPerType * typeCount);

        // Tạo danh sách items theo loại, mỗi loại chia hết cho 3
        List<NormalItem.eNormalType> itemList = new List<NormalItem.eNormalType>();
        for (int i = 0; i < typeCount; i++)
        {
            for (int j = 0; j < itemsPerType; j++)
            {
                itemList.Add(allTypes[i]);
            }
        }

        int remaining = totalCells - itemList.Count;

        int index = 0;
        while (remaining >= 3)
        {
            NormalItem.eNormalType type = allTypes[index % typeCount];

            itemList.Add(type);
            itemList.Add(type);
            itemList.Add(type);

            remaining -= 3;
            index++;
        }

        int itemIndex = 0;
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                NormalItem item = new NormalItem();

                item.SetType(itemList[itemIndex]);
                itemIndex++;

                item.SetView();
                item.SetViewRoot(m_root);

                cell.Assign(item);
                cell.ApplyItemPosition(false);

                
            }
        }
    }

    internal void Shuffle()
    {
        List<Item> list = new List<Item>();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                list.Add(m_cells[x, y].Item);
                m_cells[x, y].Free();
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                int rnd = UnityEngine.Random.Range(0, list.Count);
                m_cells[x, y].Assign(list[rnd]);
                m_cells[x, y].SetItemInit(list[rnd]);
                m_cells[x, y].ApplyItemMoveToPosition();

                list.RemoveAt(rnd);
            }
        }
    }

    public List<Cell> GetHorizontalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        //check horizontal match
        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourRight;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourLeft;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }


    public List<Cell> GetVerticalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourUp;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourBottom;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }

    internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
    {
        eMatchDirection dir = GetMatchDirection(matches);

        BonusItem item = new BonusItem();
        switch (dir)
        {
            case eMatchDirection.ALL:
                item.SetType(BonusItem.eBonusType.ALL);
                break;
            case eMatchDirection.HORIZONTAL:
                item.SetType(BonusItem.eBonusType.HORIZONTAL);
                break;
            case eMatchDirection.VERTICAL:
                item.SetType(BonusItem.eBonusType.VERTICAL);
                break;
        }

        if (item != null)
        {
            if (cellToConvert == null)
            {
                int rnd = UnityEngine.Random.Range(0, matches.Count);
                cellToConvert = matches[rnd];
            }

            item.SetView();
            item.SetViewRoot(m_root);

            cellToConvert.Free();
            cellToConvert.Assign(item);
            cellToConvert.ApplyItemPosition(true);
        }
    }


    internal eMatchDirection GetMatchDirection(List<Cell> matches)
    {
        if (matches == null || matches.Count < m_matchMin) return eMatchDirection.NONE;

        var listH = matches.Where(x => x.BoardX == matches[0].BoardX).ToList();
        if (listH.Count == matches.Count)
        {
            return eMatchDirection.VERTICAL;
        }

        var listV = matches.Where(x => x.BoardY == matches[0].BoardY).ToList();
        if (listV.Count == matches.Count)
        {
            return eMatchDirection.HORIZONTAL;
        }

        if (matches.Count > 5)
        {
            return eMatchDirection.ALL;
        }

        return eMatchDirection.NONE;
    }

    internal List<Cell> FindFirstMatch()
    {
        List<Cell> list = new List<Cell>();

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                var listhor = GetHorizontalMatches(cell);
                if (listhor.Count >= m_matchMin)
                {
                    list = listhor;
                    break;
                }

                var listvert = GetVerticalMatches(cell);
                if (listvert.Count >= m_matchMin)
                {
                    list = listvert;
                    break;
                }
            }
        }

        return list;
    }

    public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
    {
        var dir = GetMatchDirection(matches);

        var bonus = matches.Where(x => x.Item is BonusItem).FirstOrDefault();
        if (bonus == null)
        {
            return matches;
        }

        List<Cell> result = new List<Cell>();
        switch (dir)
        {
            case eMatchDirection.HORIZONTAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.VERTICAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.ALL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
                    {
                        result.Add(cell);
                    }
                }
                break;
        }

        return result;
    }

    internal void ShiftDownItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            int shifts = 0;
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell.IsEmpty)
                {
                    shifts++;
                    continue;
                }

                if (shifts == 0) continue;

                Cell holder = m_cells[x, y - shifts];

                Item item = cell.Item;
                cell.Free();

                holder.Assign(item);
                item.View.DOMove(holder.transform.position, 0.3f);
            }
        }
    }

    public void Clear()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.Clear();

                GameObject.Destroy(cell.gameObject);
                m_cells[x, y] = null;
            }
        }
    }

    /// New methods for CheckRow mechanics
    public void MoveItemToCheckRow(Cell sourceCell)
    {
        if (sourceCell == null || sourceCell.IsEmpty) return;

        Item item = sourceCell.Item;
        sourceCell.Free();

        int insertIndex = -1;
        for (int i = 0; i < 5; i++)
        {
            if (m_checkRow[i, 1] == null || m_checkRow[i, 1].IsEmpty)
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex == -1) return;


        Cell checkRowCell = m_checkRow[insertIndex, 1];
        checkRowCell.Assign(item);
    }

    public IEnumerator CheckRowMatchAndCollapse()
    {

        OrganizeCheckRowByType();

        yield return new WaitForSeconds(0.1f);

        List<Cell> matchedCells = CheckRowForMatches();

        if (matchedCells.Count >= m_matchMin)
        {
            // Clear matched items
            foreach (var cell in matchedCells)
            {
                cell.ExplodeItem();
            }

            ShiftCheckRowItemsLeft();
        }

        yield return new WaitForSeconds(0.1f);

    }

    private void OrganizeCheckRowByType()
    {
        // Collect tất cả items trong CheckRow 
        List<Item> items = new List<Item>();
        for (int i = 0; i < 5; i++)
        {
            if (m_checkRow[i, 1] != null && !m_checkRow[i, 1].IsEmpty)
            {
                items.Add(m_checkRow[i, 1].Item);
                m_checkRow[i, 1].Free();
            }
        }

        // Sắp xếp items theo type
        items = items.OrderBy(x =>
        {
            if (x is NormalItem normalItem)
                return (int)normalItem.ItemType;
            return 999;
        }).ToList();

        // Gán items lại 
        for (int i = 0; i < items.Count && i < 5; i++)
        {
            m_checkRow[i, 1].Assign(items[i]);
            items[i].View.DOMove(m_checkRow[i, 1].transform.position, 0.2f);
        }
    }

    private List<Cell> CheckRowForMatches()
    {
        List<Cell> matches = new List<Cell>();

        for (int i = 0; i < 5; i++)
        {
            Cell cell = m_checkRow[i, 1];
            if (cell == null || cell.IsEmpty) continue;

            int matchCount = 1;
            int j = i + 1;

            
            while (j < 5 && m_checkRow[j, 1] != null && !m_checkRow[j, 1].IsEmpty
                && m_checkRow[j, 1].IsSameType(cell))
            {
                matchCount++;
                j++;
            }

            // Nếu match >= 3
            if (matchCount >= m_matchMin)
            {
                for (int k = i; k < i + matchCount; k++)
                {
                    if (!matches.Contains(m_checkRow[k, 1]))
                    {
                        matches.Add(m_checkRow[k, 1]);
                    }
                }
            }
        }

        return matches;
    }

    private void ShiftCheckRowItemsLeft()
    {
        List<Item> items = new List<Item>();

  
        for (int i = 0; i < 5; i++)
        {
            if (m_checkRow[i, 1] != null && !m_checkRow[i, 1].IsEmpty)
            {
                items.Add(m_checkRow[i, 1].Item);
                m_checkRow[i, 1].Free();
            }
        }

        for (int i = 0; i < items.Count && i < 5; i++)
        {
            m_checkRow[i, 1].Assign(items[i]);
            items[i].View.DOMove(m_checkRow[i, 1].transform.position, 0.2f);
        }
    }

    public bool IsCheckRowFull()
    {
        for (int i = 0; i < 5; i++)
        {
            if (m_checkRow[i, 1] == null || m_checkRow[i, 1].IsEmpty)
                return false;
        }
        return true;
    }

    public int GetCheckRowFilledCount()
    {
        int count = 0;
        for (int i = 0; i < 5; i++)
        {
            if (m_checkRow[i, 1] != null && !m_checkRow[i, 1].IsEmpty)
                count++;
        }
        return count;
    }

    public void ClearCheckRow()
    {
        for (int i = 0; i < 5; i++)
        {
            if (m_checkRow[i, 1] != null)
            {
                m_checkRow[i, 1].Clear();
            }
        }
    }

    public bool IsBoardEmpty()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (!m_cells[x, y].IsEmpty)
                    return false;
            }
        }
        return true;
    }

    public Cell GetCell(int x, int y)
    {
        if (x >= 0 && x < boardSizeX && y >= 0 && y < boardSizeY)
        {
            return m_cells[x, y];
        }
        return null;
    }

    public Cell GetCheckRowCell(int index)
    {
        if (index >= 0 && index < 5)
        {
            return m_checkRow[index, 1];
        }
        return null;
    }

    public bool ReturnItemFromCheckRow(int checkRowIndex)
    {


        Cell checkRowCell = GetCheckRowCell(checkRowIndex);
        if (checkRowCell == null || checkRowCell.IsEmpty)
            return false;

        Item item = checkRowCell.Item;
        Cell targetCell = item.initCell;

        UnityEngine.Debug.Log("ReturnItemFromCheckRow" + targetCell.BoardX + "," + targetCell.BoardY);

        if (targetCell != null)
        {
            checkRowCell.Free();
            targetCell.Assign(item);
            item.View.DOMove(targetCell.transform.position, 0.3f);
           
            // Shift CheckRow items to the left
            ShiftCheckRowItemsLeft();
            return true;
        }

        return false;
    }}