using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private Camera m_cam;

    private GameSettings m_gameSettings;
    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;

    private bool m_isAutoPlay;

    private bool m_autoPlayStarted;

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_isAutoPlay = (gameSettings.CurrentLevelMode == GameManager.eLevelMode.AUTOPLAY || 
                        gameSettings.CurrentLevelMode == GameManager.eLevelMode.AUTOLOSE);

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        Fill();
        m_board.Shuffle();
    }

    private void Fill()
    {
        m_board.Fill();

    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                StopHints();
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint)
            {
                m_timeAfterFill = 0f;
                ShowHint();
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Cell cell = hit.collider.GetComponent<Cell>();
                if (cell != null && !cell.IsEmpty)
                {
                    // Check if clicked cell is in CheckRow
                    bool isInCheckRow = false;
                    int checkRowIndex = -1;
                    
                    for (int i = 0; i < 5; i++)
                    {
                        if (m_board.GetCheckRowCell(i) == cell)
                        {
                            isInCheckRow = true;

                            Debug.Log("Clicked cell is in CheckRow at index: " + i);

                            checkRowIndex = i;
                            break;
                        }
                    }

                    // If in CheckRow and TIMER mode, return item to original position
                    if (isInCheckRow && m_gameSettings.CurrentLevelMode == GameManager.eLevelMode.TIMER)
                    {
                        IsBusy = true;
                        if (m_board.ReturnItemFromCheckRow(checkRowIndex))
                        {
                            StartCoroutine(WaitForReturnAnimation());
                        }
                        else
                        {
                            IsBusy = false;
                        }
                        return;
                    }

                    // Check lose condition - CheckRow full (only for non-TIMER modes)
                    if (m_board.IsCheckRowFull() && m_gameSettings.CurrentLevelMode != GameManager.eLevelMode.TIMER)
                    {
                        m_gameManager.SetState(GameManager.eStateGame.GAME_OVER);
                        return;
                    }

                    StopHints();
                    IsBusy = true;
                    m_board.MoveItemToCheckRow(cell);

                    StartCoroutine(WaitForCheckRowAnimation());

                    // Start autoplay on first click
                    if (m_isAutoPlay && !m_autoPlayStarted)
                    {
                        m_autoPlayStarted = true;
                        StartCoroutine(AutoPlayCoroutine());
                    }
                }
            }
        }

    }

    private IEnumerator WaitForCheckRowAnimation()
    {
        StartCoroutine(m_board.CheckRowMatchAndCollapse());

        yield return new WaitForSeconds(0.5f);

        if (m_board.IsCheckRowFull() && m_gameSettings.CurrentLevelMode != GameManager.eLevelMode.TIMER)
        {
            m_gameManager.SetState(GameManager.eStateGame.GAME_OVER);
        }

        // Check win condition - board empty
        if (m_board.IsBoardEmpty())
        {
            m_gameManager.SetState(GameManager.eStateGame.GAME_WIN);
        }

        IsBusy = false;
    }

    private IEnumerator WaitForReturnAnimation()
    {
        yield return new WaitForSeconds(0.3f);
        IsBusy = false;
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }



    internal void Clear()
    {
        m_board.Clear();
    }

    private void ShowHint()
    {
        return;
    }

    private void StopHints()
    {
        return;
    }

    private IEnumerator AutoPlayCoroutine()
    {
        while (!m_gameOver)
        {
            yield return new WaitForSeconds(0.5f);

            // Get all available cells
            List<Cell> availableCells = new List<Cell>();
            for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
            {
                for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
                {
                    Cell cell = m_board.GetCell(x, y);
                    if (cell != null && !cell.IsEmpty)
                    {
                        availableCells.Add(cell);
                    }
                }
            }

            if (availableCells.Count == 0) break;

            // Choose cell based on mode
            Cell selectedCell = null;

            if (m_gameSettings.CurrentLevelMode == GameManager.eLevelMode.AUTOPLAY)
            {
                // Play to win - select cell that matches types in CheckRow
                selectedCell = GetBestCellForAutoPlay(availableCells);
            }
            else if (m_gameSettings.CurrentLevelMode == GameManager.eLevelMode.AUTOLOSE)
            {
                // Play to lose - select cell that will fill CheckRow
                // Prefer the first available cell to quickly fill CheckRow
                selectedCell = availableCells[0];
            }

            if (selectedCell != null)
            {
                IsBusy = true;

                // Check if CheckRow is full
                if (m_board.IsCheckRowFull())
                {
                    m_gameManager.SetState(GameManager.eStateGame.GAME_OVER);
                    break;
                }

                m_board.MoveItemToCheckRow(selectedCell);
                StartCoroutine(WaitForCheckRowAnimation());

                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                break;
            }
        }
    }

    private Cell GetBestCellForAutoPlay(List<Cell> availableCells)
    {
        if (availableCells.Count == 0) return null;


        List<NormalItem.eNormalType> checkRowTypes = new List<NormalItem.eNormalType>();
        for (int i = 0; i < 5; i++)
        {
            Cell checkCell = m_board.GetCheckRowCell(i);
            if (checkCell != null && !checkCell.IsEmpty)
            {
                NormalItem item = checkCell.Item as NormalItem;
                if (item != null && !checkRowTypes.Contains(item.ItemType))
                {
                    checkRowTypes.Add(item.ItemType);
                }
            }
        }


        if (checkRowTypes.Count == 0)
        {
            return availableCells[UnityEngine.Random.Range(0, availableCells.Count)];
        }


        List<Cell> matchingCells = new List<Cell>();
        foreach (Cell cell in availableCells)
        {
            NormalItem item = cell.Item as NormalItem;
            if (item != null && checkRowTypes.Contains(item.ItemType))
            {
                matchingCells.Add(cell);
            }
        }

        if (matchingCells.Count > 0)
        {
            return matchingCells[UnityEngine.Random.Range(0, matchingCells.Count)];
        }

        return availableCells[UnityEngine.Random.Range(0, availableCells.Count)];
    }
}
