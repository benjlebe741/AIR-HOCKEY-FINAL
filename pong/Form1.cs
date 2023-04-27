using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Media;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace pong
{
    public partial class Form1 : Form
    {
        #region Global Variables
        //KEYDOWN/KEYUP LISTS:
        bool[] WSADUpDownLeftRight = new bool[] { false, false, false, false, false, false, false, false };
        Keys[] keysToCheck = new Keys[] { Keys.W, Keys.S, Keys.A, Keys.D, Keys.Up, Keys.Down, Keys.Left, Keys.Right };


        //The first column refers to spaces on the WASDUpDownLeftRight bool[]. The second column refers to the oppoite key to column one, for example [0][0] = Up boolian value, [0][1] = Down boolian value.
        //The final column refers to the space in the "objectDirectionsXY int[]" that should be changed depending on the values in the first two columns.
        //This means I can repeat the same code for each set of keys using a for loop that checks the values in each row and finally outputs a direction.
        int[][] determineDirectionsList = new int[][]
          {
      new int[]{  0,1,4},
      new int[]{  2,3,1},
      new int[]{  4,5,5},
      new int[]{  6,7,2},
          };

        //Similarly to the determineDirectionsList, the Colision Detections List (CDL) is just a list of information used during detecting wall and paddle colisions; due to it involving screen info, it will be declared later.
        int[][] CDL = new int[][] { };
        int colisionListDifference = 6;

        //A stopwatch to do things at certain intervals.
        Stopwatch stopwatch = new Stopwatch();

        //Amount of time between each stopwatch update
        int decreaseSpeedsInterval = 40;
        int updatePositionInterval = 5;


        //BRUSHES & PENS
        SolidBrush redBrush = new SolidBrush(Color.Red);
        SolidBrush blueBrush = new SolidBrush(Color.Blue);
        SolidBrush whiteBrush = new SolidBrush(Color.White);
        Pen whitePen = new Pen(Color.White, 5);
        Pen grayPen = new Pen(Color.Gray, 2);

        //SOUNDS
        SoundPlayer gameEnd = new SoundPlayer(pong.Properties.Resources.PuckHit2);
        SoundPlayer resetPositions = new SoundPlayer(pong.Properties.Resources.PuckHit);
        SoundPlayer puckHit = new SoundPlayer(pong.Properties.Resources.PuckHit4);

        //GAME OBJECTS
        Rectangle[] movingObjects = new Rectangle[] { };
        Rectangle[] ScoreZones = new Rectangle[] { };

        //and information about game objects when they are created:
        int scoreZoneWidth = 150;
        int scoreZoneHeight = 20;

        int diskWidth = 65;
        int paddleWidth = 110;


        //TRACKING OBJECT INFORMATION: 0 = BALL 1 = PLAYER1 2 = PLAYER2
        int[] objectDirectionsXY;
        double[] objectSpeedsXY;

        //Variables to know if the player can go any further towards the puck
        int[][] canMoveUpDownLeftRight = new int[][]
        {
           new int[]{ 0, 0, 0, 0 },
           new int[]{ 0, 0, 0, 0 },
           new int[]{ 0, 0, 0, 0 }
        };

        //TRACKING INFORMATION ON ONLY THE TWO PADDLES:
        Point[] previousLocations = new Point[] { new Point(10, 10), new Point(10, 10) };
        double[] paddleVelocitiesXY;

        //SCORE INFORMATION
        int[] playerScores = new int[] { 0, 0 };
        int winScoreAmount = 3;

        //Actions to either add to or set a score (this will be used depending on if I add or change an objects poition on colision)
        Action<int, int, int> setValue;
        Action<int, int, int> addValue;
        #endregion

        #region Setting Up Game
        public Form1()
        {
            InitializeComponent();

            //Begin the stopwatch which does things in different intervals.
            stopwatch.Start();

            //Declare score zones based on screen dimensions.
            ScoreZones = new Rectangle[] //0 = TopZone(player2Net) 1 = BottomZone(player1Net)
          {
          new Rectangle(this.Width / 2 - scoreZoneWidth / 2, this.Top , scoreZoneWidth, scoreZoneHeight),
          new Rectangle(this.Width / 2 - scoreZoneWidth / 2, this.Bottom - scoreZoneHeight, scoreZoneWidth, scoreZoneHeight)
          };

            //COLISION DETAILS LIST: Each row is a new type of colision check,
            //#0 and #1: is this measuring the X or Y axis? 0 = dont count this axis, 1 = do count this axis.
            //#2: is this less than or greater than? 1 = less than, -1 = greater than.
            //#3: what is the axis greater or less than?
            //4:UpDownLeftRight(_)
            CDL = new int[][] {
                //Colision with walls: 
                //5: does this affect everything or just one object? if everything CDL[][5] = 99
                     new int[]{0, 1, 1, 0, 0, 99},
                     new int[]{0, 1,-1, this.Height, 1, 99},
                     new int[]{1, 0, 1, 0, 2, 99},
                     new int[]{1, 0,-1, this.Width, 3, 99},
                     new int[]{0, 1,-1, this.Height / 2, 1, 1},
                     new int[]{0, 1,1, this.Height / 2, 0, 2},
                 //Colision with players:
                     new int[]{0, 1, 1, 0, 1},
                     new int[]{0, 1,-1, 0, 0},
                     new int[]{1, 0, 1, 0, 3},
                     new int[]{1, 0,-1, 0, 2},
                    };

            //Set up actions (I found out about actions, functions, deligates, and predicates, and I really want to use them)
            setValue = setObjectCoordinate;
            addValue = addObjectCoordinate;

            //Set all positions for paddles and balls based on screen dimensions.
            ResetPositions();
        }
        #endregion

        private void gameTimer_Tick(object sender, EventArgs e)
        {
            #region Check Watch Intervals
            //At a certain interval decrease the speed of the ball over time as it looses velocity.
            if (WatchIntervalIs(decreaseSpeedsInterval))
            {
                for (int i = 0; i < 6; i += 3)
                {
                    if (objectSpeedsXY[i] > 0.5) { objectSpeedsXY[i] -= 0.5; }
                }
            }

            //At a certain interval determine both paddles velocities by comparing their current position to the past position. (previousLocations only has the two paddles, while movingObjects starts with the ball, so I add one to i)
            if (WatchIntervalIs(updatePositionInterval))
            {
                for (int i = 0; i < 2; i++)
                {
                    paddleVelocitiesXY[i] = Math.Abs(movingObjects[i + 1].X - previousLocations[i].X);
                    paddleVelocitiesXY[i + 2] = Math.Abs(movingObjects[i + 1].Y - previousLocations[i].Y);
                }
            }
            #endregion

            //For information not about the puck: (goal scoring and paddle positions)
            for (int i = 0; i < 2; i++)
            {
                //update the objects previous locations
                previousLocations[i] = movingObjects[i + 1].Location;

                #region Goal Score Check
                //Check to see if a goal has been scored.
                if (movingObjects[0].IntersectsWith(ScoreZones[i]))
                {
                    //Play a sound
                    resetPositions.Play();
                    Refresh();

                    playerScores[i]++;

                    //Has the player won the game?
                    if (playerScores[i] >= winScoreAmount)
                    {
                        //if so: Display who won, stop the game, and allow the players to restart the game.
                        winLabel.Text = $"PLAYER {i + 1} IS THE WINNER";
                        gameTimer.Enabled = false;
                        resetButton.Enabled = true;
                        resetButton.Visible = true;
                        gameEnd.Play();
                        Refresh();
                    }
                    //If someone has scored a goal reset game object positions.
                    ResetPositions();
                }
                #endregion
            }

            #region Determing All Object Directions
            //Determening all player directions depending on what keys are pressed: (for example: if 'Up' is down, and 'Down' is not, player2's vertical direction is -1, reverse those to get 1, and if both/neither are down, get a direction of 0.)
            for (int i = 0; i < determineDirectionsList.Length; i++)
            {
                if (WSADUpDownLeftRight[determineDirectionsList[i][0]] == true && WSADUpDownLeftRight[determineDirectionsList[i][1]] == false)
                {
                    objectDirectionsXY[determineDirectionsList[i][2]] = -1;
                }
                else if (WSADUpDownLeftRight[determineDirectionsList[i][1]] == true && WSADUpDownLeftRight[determineDirectionsList[i][0]] == false)
                {
                    objectDirectionsXY[determineDirectionsList[i][2]] = 1;
                }
                else
                {
                    objectDirectionsXY[determineDirectionsList[i][2]] = 0;
                }
            }
            #endregion

            //COLISIONS! 
            for (int i = 0; i <= 2; i++)
            {
                #region Update Object Positions
                //UPDATE OBJECT POSITIONS
                if (objectDirectionsXY[i] != canMoveUpDownLeftRight[i][3] && objectDirectionsXY[i] != canMoveUpDownLeftRight[i][2])
                {
                    addValue(i, Convert.ToInt32(objectDirectionsXY[i] * objectSpeedsXY[i]), 0);
                }
                if (objectDirectionsXY[i + 3] != canMoveUpDownLeftRight[i][0] && objectDirectionsXY[i + 3] != canMoveUpDownLeftRight[i][1])
                {
                    addValue(i, 0, Convert.ToInt32(objectDirectionsXY[i + 3] * objectSpeedsXY[i + 3]));
                }
                #endregion

                #region Wall Colision Check Code
                for (int j = 0; j <= 5; j++)
                {
                    #region Revised Wall Check Code
                    int yDifference = 3 * CDL[j][1];
                    //if the equation includes an objects height
                    int includedHeight = (movingObjects[i].Width * ((CDL[j][2] - 1) * -1 / 2));
                    ColisionCheck(i, i, j, CDL[j][5], CDL[j][2], CDL[j][3] - includedHeight, yDifference, 0, objectDirectionsXY[0 + yDifference] * -1, CDL[j][2] * -1, setValue, CDL[j][3] - includedHeight + (Convert.ToInt32(objectSpeedsXY[i + yDifference] * CDL[j][2])), movingObjects[i].X, movingObjects[i].Y);
                    #endregion

                    #region Second Wall Check Code
                    //if the equation includes an objects height
                    //int includedHeight = (movingObjects[i].Width * ((CDL[j][2] - 1) * -1 / 2));

                    ////First check if the moving objects X/Y is <= or >= the specified amount. 
                    //if (((movingObjects[i].X * CDL[j][0]) + (movingObjects[i].Y * CDL[j][1])) * CDL[j][2] <= (CDL[j][3] - includedHeight) * CDL[j][2] && (CDL[j][5] == i || CDL[j][5] == 99))
                    //{

                    ////variable for if the interaction affects the y axis.
                    //int yDifference = 3 * CDL[j][1];
                    ////If so and the object is the puck, change the direction the puck travels on the axis; for example if the puck hits the top/bottom wall multiply the Y direction by -1.
                    //if (i == 0)
                    //{
                    //    objectDirectionsXY[0 + yDifference] *= -1;
                    //    canMoveUpDownLeftRight[0][CDL[j][4]] = CDL[j][2] * -1;
                    //}
                    ////Affect either the X or Y coordinates of the rectangle, and set them away from the wall they ran into.
                    //if (CDL[j][0] == 1)
                    //{
                    //    setObjectCoordinate(i, CDL[j][3] - includedHeight + (Convert.ToInt32(objectSpeedsXY[i + yDifference] * CDL[j][2])), movingObjects[i].Y);
                    //}
                    //else
                    //{
                    //    setObjectCoordinate(i, movingObjects[i].X, CDL[j][3] - includedHeight + (Convert.ToInt32(objectSpeedsXY[i + yDifference] * CDL[j][2])));
                    //}
                    //}
                    #endregion
                }
                #region First Wall Check Code
                ////IF ANY OBJECTS ARE HITTING THE WALLS; PUSH THEM BACK
                ////TOP WALL
                //if (movingObjects[i].Y <= 0)
                //{
                //    if (i == 0)
                //    {
                //        objectDirectionsXY[0 + 3] *= -1;
                //        canMoveUpDownLeftRight[0][0] = -1;
                //    }
                //    movingObjects[i].Y = 0 + Convert.ToInt32(objectSpeedsXY[i + 3]);
                //}
                ////BOTTOM WALL
                //if (movingObjects[i].Y * -1 <= (this.Height - movingObjects[i].Height) * -1)
                //{
                //    if (i == 0)
                //    {
                //        objectDirectionsXY[0 + 3] *= -1;
                //        canMoveUpDownLeftRight[0][1] = 1;
                //    }
                //    movingObjects[i].Y = this.Height - movingObjects[i].Height - Convert.ToInt32(objectSpeedsXY[i + 3]);
                //}
                ////LEFT WALL
                //if (movingObjects[i].X <= 0)
                //{
                //    if (i == 0)
                //    {
                //        objectDirectionsXY[0] *= -1;
                //        canMoveUpDownLeftRight[0][2] = -1;
                //    }
                //    movingObjects[i].X = 0 + Convert.ToInt32(objectSpeedsXY[i]);
                //}
                //RIGHT WALL
                //if (movingObjects[i].X * -1 <= (this.Width - movingObjects[i].Width) * -1)
                //{
                //    if (i == 0)
                //    {
                //        objectDirectionsXY[0] *= -1;
                //        canMoveUpDownLeftRight[0][3] = 1;
                //    }
                //    movingObjects[i].X = this.Width - movingObjects[i].Width - Convert.ToInt32(objectSpeedsXY[i]);
                //}
                #endregion

                #endregion

                #region Paddle Colision Check Code
                //OBJECTS INTERACTING WITH EACHOTHER; I wont use .Intersects with because these are circles;
                //--instead I want to compare the position of the ball and the other circles by drawing a line between each circle and
                //--looking at if it is smaller than the sum of their radius's. Math.Sprt((x1-x2)^2 + (y1-y2)^2) <= r1+r2
                if (i != 0 && GetLength(movingObjects[0], movingObjects[i]) <= Convert.ToDouble((movingObjects[0].Width / 2) + (movingObjects[i].Width / 2)))
                {
                    for (int j = colisionListDifference; j <= 3 + colisionListDifference; j++)
                    {
                        #region Revised Paddle Check Code

                        //WIP:
                        int yDifference = 3 * CDL[j][1];
                        //if the equation includes an objects height
                        int includedHeight = (movingObjects[0].Width * ((CDL[j][2] - 1) * -1 / 2));
                        ColisionCheck(0, i, j, 99, CDL[j][2], (((movingObjects[i].X * CDL[j][0]) + (movingObjects[i].Y * CDL[j][1])) + includedHeight), yDifference, i, CDL[j][2] * -1, CDL[j][2], addValue, 2 * CDL[j][2], 0, 0);

                        #endregion

                        #region Second Paddle Check Code
                        ////if the equation includes an objects height
                        //int includedHeight = (movingObjects[0].Width * ((CDL[j][2] - 1) * -1 / 2));

                        ////First check if the moving objects X/Y is <= or >= the specified amount. 
                        //if (((movingObjects[0].X * CDL[j][0]) + (movingObjects[0].Y * CDL[j][1])) * CDL[j][2] <= (((movingObjects[i].X * CDL[j][0]) + (movingObjects[i].Y * CDL[j][1])) + includedHeight) * CDL[j][2])
                        //{
                        //    //variable for if the interaction affects the y axis.
                        //    int yDifference = 3 * CDL[j][1];

                        //    objectDirectionsXY[0 + yDifference] = CDL[j][2] * -1;

                        //    canMoveUpDownLeftRight[0][CDL[j][4]] = CDL[j][2];
                        //    canMoveUpDownLeftRight[i][j - colisionListDifference] = canMoveUpDownLeftRight[0][j - colisionListDifference];

                        //    //Affect either the X or Y coordinates of the rectangle, and set them away from the wall they ran into.
                        //    if (CDL[j][0] == 1)
                        //    {
                        //        addObjectCoordinate(i, 2 * CDL[j][2], 0);
                        //    }
                        //    else
                        //    {
                        //        addObjectCoordinate(i, 0, 2 * CDL[j][2]);
                        //    }
                        //}
                        #endregion

                        //Apply the velocity of the paddle to the puck when they intersect.
                        objectSpeedsXY[0] = objectSpeedsXY[0] / 2 + paddleVelocitiesXY[i - 1];
                        objectSpeedsXY[0 + 3] = objectSpeedsXY[0 + 3] / 2 + paddleVelocitiesXY[i + 2 - 1];

                    }
                    #region First Paddle Check Code
                    ////Puck Right of paddle
                    //if (movingObjects[0].X * -1 <= (movingObjects[i].X + movingObjects[0].Width) * -1)
                    //{
                    //    objectDirectionsXY[0] = 1;
                    //    canMoveUpDownLeftRight[0][2] = -1;
                    //    canMoveUpDownLeftRight[i][3] = canMoveUpDownLeftRight[0][3];

                    //    movingObjects[i].X += -2;
                    //}

                    ////Puck Left of paddle
                    //if (movingObjects[0].X <= movingObjects[i].X)
                    //{
                    //    objectDirectionsXY[0] = -1;
                    //    canMoveUpDownLeftRight[0][3] = 1;
                    //    canMoveUpDownLeftRight[i][2] = canMoveUpDownLeftRight[0][2];

                    //    movingObjects[i].X += 2;
                    //}

                    ////Puck Below paddle
                    //if (movingObjects[0].Y * -1 <= (movingObjects[i].Y + movingObjects[0].Width) * -1)
                    //{
                    //    objectDirectionsXY[0 + 3] = 1;
                    //    canMoveUpDownLeftRight[0][0] = -1;
                    //    canMoveUpDownLeftRight[i][1] = canMoveUpDownLeftRight[0][1];

                    //    movingObjects[i].Y += -2;
                    //}

                    ////Puck Above paddle
                    //if (movingObjects[0].Y <= movingObjects[i].Y)
                    //{
                    //    objectDirectionsXY[0 + 3] = -1;
                    //    canMoveUpDownLeftRight[0][1] = 1;
                    //    canMoveUpDownLeftRight[i][0] = canMoveUpDownLeftRight[0][0];

                    //    movingObjects[i].Y += 2;
                    //}


                    ////Apply the velocity of the paddle to the puck when they intersect.
                    //objectSpeedsXY[0] = objectSpeedsXY[0] / 2 + paddleVelocitiesXY[i - 1];
                    //objectSpeedsXY[0 + 3] = objectSpeedsXY[0 + 3] / 2 + paddleVelocitiesXY[i + 2 - 1];
                    #endregion
                }
                #endregion

                #region Up Down Left Right Reset
                //Reset if an object can move up or down depending on if the puck is in contact with the paddles
                if (GetLength(movingObjects[0], movingObjects[i]) > Convert.ToDouble((movingObjects[0].Width / 2) + (movingObjects[i].Width / 2)) || (i == 0 && GetLength(movingObjects[0], movingObjects[1]) > Convert.ToDouble((movingObjects[0].Width / 2) + (movingObjects[1].Width / 2)) && GetLength(movingObjects[0], movingObjects[2]) > Convert.ToDouble((movingObjects[0].Width / 2) + (movingObjects[2].Width / 2))))
                {
                    canMoveUpDownLeftRight[i][0] = 0;
                    canMoveUpDownLeftRight[i][1] = 0;
                    canMoveUpDownLeftRight[i][2] = 0;
                    canMoveUpDownLeftRight[i][3] = 0;
                }
                #endregion
            }


            Refresh();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawLine(grayPen, movingObjects[1].X + movingObjects[1].Width / 2, movingObjects[1].Y + movingObjects[1].Width / 2, ScoreZones[0].X + scoreZoneWidth / 2, ScoreZones[0].Y + scoreZoneHeight / 2);
            e.Graphics.DrawLine(grayPen, movingObjects[2].X + movingObjects[2].Width / 2, movingObjects[2].Y + movingObjects[2].Width / 2, ScoreZones[1].X + scoreZoneWidth / 2, ScoreZones[1].Y + scoreZoneHeight / 2);
            e.Graphics.FillRectangle(redBrush, ScoreZones[0]);
            e.Graphics.FillRectangle(blueBrush, ScoreZones[1]);
            e.Graphics.FillEllipse(redBrush, movingObjects[1]);
            e.Graphics.FillEllipse(blueBrush, movingObjects[2]);
            e.Graphics.FillEllipse(whiteBrush, movingObjects[0]);
        }

        #region Key Checks
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            //Check all keys and if they are down set the boolian value to true
            checkKey(true, e);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            //Check all keys and if they are up set the boolian value to false
            checkKey(false, e);
        }

        void checkKey(bool trueOrFalse, KeyEventArgs e)
        {
            for (int i = 0; i < keysToCheck.Length; i++)
            {
                if (e.KeyCode == keysToCheck[i])
                {
                    WSADUpDownLeftRight[i] = trueOrFalse;
                }
            }
        }
        #endregion

        double GetLength(Rectangle rectangle1, Rectangle rectangle2)
        {

            double x1 = rectangle1.X + (rectangle1.Width / 2);
            double x2 = rectangle2.X + (rectangle2.Width / 2);
            double y1 = rectangle1.Y + (rectangle1.Height / 2);
            double y2 = rectangle2.Y + (rectangle2.Height / 2);

            //A^2 = B^2 + C^2
            double length = Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));
            return length;
        }
       

        void ResetPositions()
        {
            movingObjects = new Rectangle[] //0 = ball, 1 = player1, 2 = player2
           {
              new Rectangle(this.Width / 2 - diskWidth / 2, this.Height / 2 - diskWidth / 2, diskWidth, diskWidth),
              new Rectangle(this.Width / 2 - paddleWidth / 2, this.Top, paddleWidth, paddleWidth),
              new Rectangle(this.Width / 2 - paddleWidth / 2, this.Bottom - paddleWidth, paddleWidth, paddleWidth)
        };
            paddleVelocitiesXY = new double[] { 0, 0, 0, 0 };
            //TRACKING OBJECT INFORMATION: 0 = BALL 1 = PLAYER1 2 = PLAYER2
            objectDirectionsXY = new int[] { -1, 0, 0, -1, 0, 0 };
            objectSpeedsXY = new double[] { 0, 7, 7, 0, 7, 7 };
            updateScores();
        }

        void updateScores()
        {
            player1ScoreLabel.Text = $"PLAYER 1: {playerScores[0]}";
            player2ScoreLabel.Text = $"PLAYER 1: {playerScores[1]}";
        }
        private void button1_Click(object sender, EventArgs e)
        {
            playerScores = new int[] { 0, 0 };
            resetButton.Enabled = false;
            resetButton.Visible = false;
            winLabel.Text = "";
            gameTimer.Enabled = true;
            updateScores();
        }

        bool WatchIntervalIs(int interval)
        {
            bool trueOrFalse = false;
            if (stopwatch.ElapsedMilliseconds % interval == 0) { trueOrFalse = true; }
            return trueOrFalse;
        }

        void setObjectCoordinate(int i, int setPositionX, int setPositionY)
        {
            movingObjects[i].X = setPositionX;
            movingObjects[i].Y = setPositionY;
        }

        void addObjectCoordinate(int i, int addPositionX, int addPositionY)
        {
            movingObjects[i].X += addPositionX;
            movingObjects[i].Y += addPositionY;
        }

        //Run based on the Colision Detections List for colisions with paddles and the wall.
        void ColisionCheck(int zeroOrI, int i, int j, int onlyAffectsI, int lesserOrGreater, int comparision, int yDifference, int onlyAffect, int changeDirection, int cannotMove_, Action<int, int, int> action, int affectPosition, int keepPositionX, int keepPositionY)
        {
            //Detect if the objects X or Y location relative to the wall or paddle is the same (or beyond where it should be) 
            if (((movingObjects[zeroOrI].X * CDL[j][0]) + (movingObjects[zeroOrI].Y * CDL[j][1])) * lesserOrGreater <= (comparision) * lesserOrGreater && (onlyAffectsI == i || onlyAffectsI == 99))
            {
                //If so: affect the puck only in some ways (like changing its direction when it hits the wall); otherwise affect everything if 'onlyAffect' is equal to 'i'
                if (i == onlyAffect)
                {
                    //Change the pucks direction, and the ability for the object to move in CDL's respective direction.
                    objectDirectionsXY[0 + yDifference] = changeDirection;
                    canMoveUpDownLeftRight[0][CDL[j][4]] = cannotMove_;

                    //There are certain things that shouldnt happen if the colision is only about the puck
                    if (onlyAffect != 0)
                    {
                        canMoveUpDownLeftRight[i][j - colisionListDifference] = canMoveUpDownLeftRight[0][j - colisionListDifference];
                    }

                    //Play a sound of the puck being hit
                    puckHit.Play();
                    Refresh();
                }

                //Affect either the X or Y coordinates of the rectangle:
                if (CDL[j][0] == 1)
                {
                    action(i, affectPosition, keepPositionY);
                }
                else
                {
                    action(i, keepPositionX, affectPosition);
                }
            }
        }
    }
}
