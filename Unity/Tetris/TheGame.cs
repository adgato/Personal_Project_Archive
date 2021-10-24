using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TheGame : MonoBehaviour
{
    [Space(10)]
    [Header("Display Elements:")]
    public GameObject Brick;
    public GameObject Hold;
    public GameObject Next;
    public GameObject Score;
    public GameObject GameOver;

    private int[,] Tiles = new int[20, 10];
    private int[,] activeCoords = new int[4, 2];

    private bool gameOver = false;

    private const float defaultMoveSpeed = 0.15f;
    private const float moveSpeedMultiplier = 1.2f;
    private float autoMoveSpeed = 0.125f;
    private float levelUpBoundary = 2;

    private float Speed = 1;
    private float updateTime = 1;
    private float nextPress = 0;
    private float coyoteTime = 0;

    private int scoreValue = 0;
    private int tetrisCombo = 0;
    private int scoreCombo = 0;

    private int brickType;
    private int heldBrickType = 0;
    private bool allowHold = true;
    private int[] bankBricks;
    private int[] nextBricks;
    private int nextBrickIndex = 1;

    private int activeX = 4;
    private int activeY = 0;
    private int activeRot = 0;

    private AudioSource audioSource;
    [Space(10)]
    [Header("Sound Effects:")]
    public AudioClip moveH;
    public AudioClip moveV;
    public AudioClip rot;
    public AudioClip rotL;
    public AudioClip rotR;
    public AudioClip hold;
    public AudioClip fall;
    public AudioClip line;
    public AudioClip singleLine;
    public AudioClip doubleLine;
    public AudioClip tripleLine;
    public AudioClip tetrisLine;
    public AudioClip levelUp;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        bankBricks = GetBrickOrder();
        nextBricks = GetBrickOrder();

        brickType = nextBricks[0];

        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                GameObject brick = Instantiate(Brick, new Vector2(-1.35f + 0.3f * x, 2.85f - 0.3f * y), transform.rotation);
                brick.transform.SetParent(transform);
            }
        }

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                GameObject holdBrick = Instantiate(Brick, new Vector2(-2.85f + 0.3f * x, 2.85f - 0.3f * y), transform.rotation);
                holdBrick.transform.SetParent(Hold.transform);

                GameObject nextBrick = Instantiate(Brick, new Vector2(1.95f + 0.3f * x, 2.85f - 0.3f * y), transform.rotation);
                nextBrick.transform.SetParent(Next.transform);
            }
        }

        RenderUiElement(Hold, 0);
        RenderUiElement(Next, nextBricks[1]);
    }

    private void Update()
    {
        if (gameOver)
            return;

        int prevActiveX = activeX;
        int prevActiveY = activeY;
        int prevActiveRot = activeRot;

        if (Input.anyKeyDown)
            autoMoveSpeed = defaultMoveSpeed;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            activeX = activeX == -1 ? 0 : activeX;

            int moveRot;
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                moveRot = (activeRot + 1) % 4;
            else
                moveRot = (activeRot + 3) % 4;

                

            if (!CheckCollided(brickType, activeX, activeY, moveRot, Tiles))
                activeRot = moveRot;
            else
            {
                int moveY = activeY;

                for (int i = 0; i < 4; i++)
                {
                    moveY = Mathf.Max(0, moveY - 1);

                    if (!CheckCollided(brickType, activeX, moveY, moveRot, Tiles))
                    {
                        activeRot = moveRot;
                        activeY = moveY;
                        updateTime = Time.time * Speed + 1;
                        break;
                    }
                }
            }
        }

        //Not else if here to allow player to rotate and move
        if (Input.GetKeyDown(KeyCode.A) || (Input.GetKey(KeyCode.A) && Time.time >= nextPress))
        {
            autoMoveSpeed = Mathf.Max(0.05f, autoMoveSpeed /moveSpeedMultiplier);
            nextPress = Time.time + autoMoveSpeed;

            int moveX = Mathf.Max((brickType == 1 && activeRot % 2 == 0) || (brickType == 2 && activeRot == 3) ? -1 : 0, activeX - 1);

            activeX = !CheckCollided(brickType, moveX, activeY, activeRot, Tiles) ? moveX : activeX;
        }
        else if (Input.GetKeyDown(KeyCode.D) || (Input.GetKey(KeyCode.D) && Time.time >= nextPress))
        {
            autoMoveSpeed = Mathf.Max(0.05f, autoMoveSpeed / moveSpeedMultiplier);
            nextPress = Time.time + autoMoveSpeed;

            int moveX = activeX + 1;

            activeX = !CheckCollided(brickType, moveX, activeY, activeRot, Tiles) ? moveX : activeX;
        }
        else if (Input.GetKeyDown(KeyCode.S) || (Input.GetKey(KeyCode.S) && Time.time >= nextPress))
        {
            autoMoveSpeed = Mathf.Max(0.05f, autoMoveSpeed / moveSpeedMultiplier);
            nextPress = Time.time + autoMoveSpeed;

            int moveY = activeY + 1;

            if (!CheckCollided(brickType, activeX, moveY, activeRot, Tiles))
            {
                activeY = moveY;
                updateTime = Time.time * Speed + 1;
            }
        }

        else if (Input.GetKeyDown(KeyCode.W))
        {

            int moveY = activeY;

            while (moveY < 20 && !CheckCollided(brickType, activeX, moveY, activeRot, Tiles))
                moveY++;

            activeY = moveY - 1;

            coyoteTime = Time.time - 0.5f;
        }
        else if (Input.GetKeyDown(KeyCode.LeftShift) && allowHold)
        {
            audioSource.PlayOneShot(hold);

            allowHold = false;

            if (heldBrickType == 0)
            {
                (brickType, heldBrickType) = (nextBrickIndex == 0 ? nextBricks[0] : nextBricks[nextBrickIndex - 1], brickType);
                NewBrick();
            }
            else
            {
                (brickType, heldBrickType) = (heldBrickType, brickType);

                activeX = 4;
                activeY = 0;
                activeRot = 0;
            }

            RenderUiElement(Hold, heldBrickType);
        }

        //Push back from the right edge if neccessary
        int[,] simulation = GetActiveCoords(brickType, activeX, activeY, activeRot);
        while (Overflow(simulation) && activeX != 0)
        {
            if (CheckCollided(brickType, activeX - 1, activeY, activeRot, Tiles)) //Only if pushing back from edge doesn't result in overlapping bricks
                break;

            activeX--;
            simulation = GetActiveCoords(brickType, activeX, activeY, activeRot);
        }

        if (prevActiveX != activeX && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)))
            audioSource.PlayOneShot(moveH);
        else if (prevActiveY != activeY && Input.GetKey(KeyCode.S))
            audioSource.PlayOneShot(moveV);
        if ((prevActiveRot + 1) % 4 == activeRot && Input.GetKey(KeyCode.LeftArrow))
        {
            audioSource.PlayOneShot(rot);
            audioSource.PlayOneShot(rotL, 0.2f);
        }
        else if ((prevActiveRot + 3) % 4 == activeRot && Input.GetKey(KeyCode.RightArrow))
        {
            audioSource.PlayOneShot(rot);
            audioSource.PlayOneShot(rotR, 0.2f);
        }     
    }

    void FixedUpdate()
    {
        if (gameOver)
            return;

        int[,] activeCoords = GetActiveCoords(brickType, activeX, activeY, activeRot);

        Tiles = SetCoords(brickType, activeCoords, Tiles);

        Render(Tiles);

        //Clear the active brick from Tiles to make collision detection with other bricks easier
        Tiles = SetCoords(0, activeCoords, Tiles);
        

        if (CheckGrounded(activeCoords))
        {
            if (activeY == 0)
            {
                System.Array.Clear(Tiles, 0, Tiles.Length);
                Render(Tiles);
                gameOver = true;
                GameOver.GetComponent<TextMesh>().text = "Game Over";
                GameOver.SetActive(true);

                StreamReader reader = new StreamReader("Assets/HighScore.txt");
                int highScore = int.Parse(reader.ReadToEnd());
                reader.Close();

                if (scoreValue > highScore)
                {
                    highScore = scoreValue;
                    StreamWriter writer = new StreamWriter("Assets/HighScore.txt", false);
                    writer.WriteLine(highScore);
                    writer.Close();
                }
                Score.GetComponent<TextMesh>().text = "Score: " + scoreValue + "\nLevel: " + (1 + (Speed - 1) * 4) + "\nHigh Score: " + highScore;
            }
                
            //Allow the player to shuffle the active brick around on top of the others
            else if (coyoteTime == 0)
                coyoteTime = Time.time + 0.5f;

            //If the player has stopped shuffling fix brick in place and create new brick at top
            else if (Time.time >= coyoteTime)
            {
                Tiles = SetCoords(brickType, activeCoords, Tiles);
                int lines_cleared;
                (Tiles, lines_cleared) = ClearFullLines(Tiles);

                if (lines_cleared == 0)
                    scoreCombo = 0;
                else
                {
                    audioSource.PlayOneShot(line);
                    if (lines_cleared != 4)
                        tetrisCombo = 0;
                }
                    

                scoreCombo++;

                if (lines_cleared == 4)
                {
                    audioSource.PlayOneShot(tetrisLine, 0.4f);
                    tetrisCombo++;
                }
                else if (lines_cleared == 3)
                    audioSource.PlayOneShot(tripleLine, 0.4f);
                else if (lines_cleared == 2)
                    audioSource.PlayOneShot(doubleLine, 0.4f);
                else if (lines_cleared == 1)
                    audioSource.PlayOneShot(singleLine, 0.4f);

                audioSource.PlayOneShot(fall);

                int prevScoreValue = scoreValue;
                scoreValue += (int)Mathf.Pow(lines_cleared, 2) * Mathf.Max(1, tetrisCombo) * scoreCombo * (int)(1 + (Speed - 1) * 2);

                Speed += (Mathf.Floor(scoreValue / (10 * (int)(1 + (Speed - 1) * 4))) - Mathf.Floor(prevScoreValue / (10 * (int)(1 + (Speed - 1) * 4)))) / 4;

                if (Speed >= levelUpBoundary)
                {
                    levelUpBoundary = Speed + 1;
                    audioSource.PlayOneShot(levelUp);
                }
                    

                Score.GetComponent<TextMesh>().text = "Score: " + scoreValue + "\nLevel: " + (1 + (Speed - 1) * 4);

                NewBrick();

                coyoteTime = 0;
                updateTime = Time.time * Speed + 1;

                allowHold = true;
            }

        }
        else
        {
            RenderDropPreview(brickType, activeX, activeY, activeRot, Tiles);
            coyoteTime = 0;
        }
            

        //Move the active block down by one
        if (Time.time * Speed >= updateTime && coyoteTime == 0)
        {
            updateTime = Time.time * Speed + 1;
            activeY++;
        }
    }

    public void NewBrick()
    {
        //Yeah, more globals here :)
        autoMoveSpeed = defaultMoveSpeed;

        brickType = nextBricks[nextBrickIndex];
        if (nextBrickIndex % 7 == 0)
        {
            nextBricks = bankBricks;
            bankBricks = GetBrickOrder();
        }

        nextBrickIndex = (nextBrickIndex + 1) % 7;

        RenderUiElement(Next, nextBricks[nextBrickIndex]);

        activeX = 4;
        activeY = 0;
        activeRot = 0;
    }

    public (int[,], int) ClearFullLines(int[,] Tiles)
    {
        int lines_cleared = 0;

        for (int line = 0; line < 20; line++)
        {
            bool clear_line = true;
            for (int x = 0; x < 10; x++)
            {
                if (Tiles[line, x] == 0)
                {
                    clear_line = false;
                    break;
                }
            }
            if (clear_line)
            {
                lines_cleared++;
                for (int y = line; y > 0; y--)
                {
                    for (int x = 0; x < 10; x++)
                        Tiles[y, x] = Tiles[y - 1, x];
                }
                for (int x = 0; x < 10; x++)
                    Tiles[0, x] = 0;
            }
        }
        return (Tiles, lines_cleared);
    }

    public bool CheckGrounded(int[,] activeCoords)
    {
        for (int coord = 0; coord < 4; coord++)
        {
            if (activeCoords[coord, 1] + 1 > 19 || Tiles[activeCoords[coord, 1] + 1, activeCoords[coord, 0]] != 0)
                return true;
        }
        return false;
    }

    public bool CheckCollided(int brickType, int moveX, int moveY, int moveRot, int[,] Tiles)
    {
        int[,] simulation = GetActiveCoords(brickType, moveX, moveY, moveRot);

        for (int coord = 0; coord < 4; coord++)
        {
            if (simulation[coord, 1] > 19)
                return true;
            else if (simulation[coord, 0] > 9)
                continue;
            else if (simulation[coord, 1] > 19 || Tiles[simulation[coord, 1], simulation[coord, 0]] != 0)
                return true;
        }
        //Yeah I'm using a global variable here, I'm sorry but this saves too many lines
        coyoteTime = Time.time * Speed + 1;
        return false;
    }

    public bool Overflow(int[,] simulation)
    {
        for (int coord = 0; coord < 4; coord++)
        {
            if (simulation[coord, 0] > 9)
                return true;
        }
        return false;
    }

    public void RenderUiElement(GameObject Element, int heldBrickType)
    {
        Color[] colours = new Color[] {
            new Color(0, 0, 0),    //0 -> no brick here
            new Color(0, 1, 1),    //1 -> cyan   I
            new Color(0.5f, 0, 1), //2 -> purple T
            new Color(1, 0.5f, 0), //3 -> orange L
            new Color(0, 0, 1),    //4 -> blue   J
            new Color(1, 0, 0),    //5 -> red    Z
            new Color(0, 1, 0),    //6 -> green  S
            new Color(1, 1, 0)     //7 -> yellow O
        };

        int[,] holdCoords = GetActiveCoords(heldBrickType, 0, 0, 0);

        for (int i = 0; i < 16; i++)
            Element.transform.GetChild(i).gameObject.GetComponent<SpriteRenderer>().color = colours[0];

        for (int i = 0; i < 4; i++)
            Element.transform.GetChild(4 * holdCoords[i, 1] + holdCoords[i, 0]).gameObject.GetComponent<SpriteRenderer>().color = colours[heldBrickType];
    }

    public void RenderDropPreview(int brickType, int activeX, int previewY, int activeRot, int[,] Tiles)
    {

        int[,] activeCoords = GetActiveCoords(brickType, activeX, previewY, activeRot);

        int moveY = previewY;

        while (moveY < 20 && !CheckCollided(brickType, activeX, moveY, activeRot, Tiles))
            moveY++;
        previewY = moveY - 1;

        int[,] previewCoords = GetActiveCoords(brickType, activeX, previewY, activeRot);

        for (int i = 0; i < 4; i++)
        {
            bool skip = false;
            for (int j = 0; j < 4; j++)
            {
                if ((previewCoords[i, 1], previewCoords[i, 0]) == (activeCoords[j, 1], activeCoords[j, 0]))
                {
                    skip = true;
                    break;
                }
            }
            if (!skip)
                transform.GetChild(10 * previewCoords[i, 1] + previewCoords[i, 0]).gameObject.GetComponent<SpriteRenderer>().color = new Color(0.2f, 0.2f, 0.2f);
        }
    }

    public void Render(int[,] Tiles)
    {
        Color[] colours = new Color[] {
            new Color(0, 0, 0),    //0 -> no brick here
            new Color(0, 1, 1),    //1 -> cyan   I
            new Color(0.5f, 0, 1), //2 -> purple T
            new Color(1, 0.5f, 0), //3 -> orange L
            new Color(0, 0, 1),    //4 -> blue   J
            new Color(1, 0, 0),    //5 -> red    Z
            new Color(0, 1, 0),    //6 -> green  S
            new Color(1, 1, 0)     //7 -> yellow O  
        };

        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 10; x++)
                transform.GetChild(10 * y + x).gameObject.GetComponent<SpriteRenderer>().color = colours[Tiles[y, x]];
        }
    }

    public int[,] GetActiveCoords(int brickType, int x, int y, int rot)
    {
        //cyan I
        if (brickType == 1)
        {
            if (rot % 2 == 0)
            {
                activeCoords = new int[4, 2] {
                    {x + 1, y},
                    {x + 1, y + 1},
                    {x + 1, y + 2},
                    {x + 1, y + 3}
                };
            }
            else
            {
                activeCoords = new int[4, 2] {
                    {x, y + 1},
                    {x + 1, y + 1},
                    {x + 2, y + 1},
                    {x + 3, y + 1}
                };
            }
        }

        //purple T
        else if (brickType == 2)
        {
            if (rot == 0)
            {
                activeCoords = new int[4, 2] {
                    {x, y + 1},
                    {x + 1, y + 1},
                    {x + 2, y + 1},
                    {x + 1, y + 2}
                };
            }
            else if (rot == 1)
            {
                activeCoords = new int[4, 2] {
                    {x + 1, y},
                    {x, y + 1},
                    {x + 1, y + 1},
                    {x + 1, y + 2}
                };
            }
            else if (rot == 2)
            {
                activeCoords = new int[4, 2] {
                    {x + 1, y},
                    {x, y + 1},
                    {x + 1, y + 1},
                    {x + 2, y + 1}
                };
            }
            else
            {
                activeCoords = new int[4, 2] {
                    {x + 1, y},
                    {x + 1, y + 1},
                    {x + 2, y + 1},
                    {x + 1, y + 2}
                };
            }
        }

        //orange L
        else if (brickType == 3)
        {
            if (rot == 0)
            {
                activeCoords = new int[4, 2] {
                    {x, y},
                    {x + 1, y},
                    {x + 2, y},
                    {x, y + 1}
                };
            }
            else if (rot == 1)
            {
                activeCoords = new int[4, 2] {
                    {x, y},
                    {x + 1, y},
                    {x + 1, y + 1},
                    {x + 1, y + 2}
                };
            }
            else if (rot == 2)
            {
                activeCoords = new int[4, 2] {
                    {x + 2, y},
                    {x, y + 1},
                    {x + 1, y + 1},
                    {x + 2, y + 1}
                };
            }
            else
            {
                activeCoords = new int[4, 2] {
                    {x, y},
                    {x, y + 1},
                    {x, y + 2},
                    {x + 1, y + 2}
                };
            }
        }

        //blue J
        else if (brickType == 4)
        {
            if (rot == 0)
            {
                activeCoords = new int[4, 2] {
                    {x, y},
                    {x + 1, y},
                    {x + 2, y},
                    {x + 2, y + 1}
                };
            }
            else if (rot == 1)
            {
                activeCoords = new int[4, 2] {
                    {x + 1, y},
                    {x + 1, y + 1},
                    {x, y + 2},
                    {x + 1, y + 2}
                };
            }
            else if (rot == 2)
            {
                activeCoords = new int[4, 2] {
                    {x, y},
                    {x, y + 1},
                    {x + 1, y + 1},
                    {x + 2, y + 1}
                };
            }
            else
            {
                activeCoords = new int[4, 2] {
                    {x, y},
                    {x + 1, y},
                    {x, y + 1},
                    {x, y + 2}
                };
            }
        }

        //red Z
        else if (brickType == 5)
        {
            if (rot % 2 == 0)
            {
                activeCoords = new int[4, 2] {
                    {x, y},
                    {x + 1, y},
                    {x + 1, y + 1},
                    {x + 2, y + 1}
                };
            }
            else
            {
                activeCoords = new int[4, 2] {
                    {x + 1, y},
                    {x, y + 1},
                    {x + 1, y + 1},
                    {x, y + 2}
                };
            }
        }

        //green S
        else if (brickType == 6)
        {
            if (rot % 2 == 0)
            {
                activeCoords = new int[4, 2] {
                    {x + 1, y},
                    {x + 2, y},
                    {x, y + 1},
                    {x + 1, y + 1}
                };
            }
            else
            {
                activeCoords = new int[4, 2] {
                    {x, y},
                    {x, y + 1},
                    {x + 1, y + 1},
                    {x + 1, y + 2}
                };
            }
        }

        //yellow O
        else
        {
            activeCoords = new int[4, 2] {
                    {x, y},
                    {x + 1, y},
                    {x, y + 1},
                    {x + 1, y + 1}
                };
        }

        return activeCoords;
    }

    public int[,] SetCoords(int brickType, int[,] Coords, int[,] Tiles)
    {
        for (int coord = 0; coord < 4; coord++)
            Tiles[Coords[coord, 1], Coords[coord, 0]] = brickType;

        return Tiles;
    }

    public int[] GetBrickOrder()
    {
        int[] nextBricks = new int[7] { 0, 0, 0, 0, 0, 0, 0 };
        for (int i = 0; i < 7; i++)
        {
            int nextBrick = 0;
            bool unique = false;
            while (!unique)
            {
                nextBrick = Random.Range(1, 8);
                unique = true;
                foreach (int brick in nextBricks)
                {
                    if (nextBrick == brick)
                    {
                        unique = false;
                        break;
                    }
                }
            }
            nextBricks[i] = nextBrick;
        }
        return nextBricks;
    }
}