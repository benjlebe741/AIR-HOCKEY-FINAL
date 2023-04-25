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


        //A stopwatch to do things at certain intervals.
        Stopwatch stopwatch = new Stopwatch();
        
        //Amount of time between each stopwatch update
        int decreaseSpeedsInterval = 20;
        int updatePositionInterval = 4;


        //BRUSHES & PENS
        SolidBrush redBrush = new SolidBrush(Color.Red);
        SolidBrush blueBrush = new SolidBrush(Color.Blue);
        SolidBrush whiteBrush = new SolidBrush(Color.White);
        Pen whitePen = new Pen(Color.White, 5);


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
        int[] canMoveUp = new int[] { 0, 0, 0 };
        int[] canMoveDown = new int[] { 0, 0, 0 };
        int[] canMoveLeft = new int[] { 0, 0, 0 };
        int[] canMoveRight = new int[] { 0, 0, 0 };

        //TRACKING INFORMATION ON ONLY THE TWO PADDLES:
        Point[] previousLocations = new Point[] { new Point(10, 10), new Point(10, 10) };
        double[] paddleVelocitiesXY;

        //SCORE INFORMATION
        int[] playerScores = new int[] { 0, 0 };
        int winScoreAmount = 3;
        #endregion
        public Form1()
        {
            InitializeComponent();
            //Begin the stopwatch which does things in different intervals.
            stopwatch.Start();
            
            //Declare score zones based on screen dimensions.
            ScoreZones = new Rectangle[] //0 = TopZone(player2Score) 1 = BottomZone(player1Score)
          {
          new Rectangle(this.Width / 2 - scoreZoneWidth / 2, this.Top , scoreZoneWidth, scoreZoneHeight),
          new Rectangle(this.Width / 2 - scoreZoneWidth / 2, this.Bottom - scoreZoneHeight, scoreZoneWidth, scoreZoneHeight)
          };

            //Set all positions for paddles and balls based on screen dimensions.
            ResetPositions();
        }

        private void gameTimer_Tick(object sender, EventArgs e)
        {
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

                for (int i = 0; i < 2; i++)
            {

                //update the objects previous locations
                previousLocations[i] = movingObjects[i + 1].Location;

                //Check to see if a goal has been scored.
                if (movingObjects[0].IntersectsWith(ScoreZones[i]))
                {
                    playerScores[i]++;
                    
                    //Has the player won the game?
                    if (playerScores[i] >= winScoreAmount)
                    {
                        //if so: Display who won, stop the game, and allow the players to restart the game.
                        winLabel.Text = $"PLAYER {i + 1} IS THE WINNER";
                        gameTimer.Enabled = false;
                        resetButton.Enabled = true;
                        resetButton.Visible = true;
                    }
                    //If someone has scored a goal reset game object positions.
                    ResetPositions();
                }
            }

            //Determening all player directions depending on what keys are pressed: (for example: if 'Up' is down, and 'Down' is not, player2's vertical direction is -1, reverse those to get 1, and if both/neither are down, get a direction of 0.)
            for (int i = 0; i < determineDirectionsList.Length; i++)
            {
                if (WSADUpDownLeftRight[determineDirectionsList[i][0]] == true && WSADUpDownLeftRight[determineDirectionsList[i][1]] == false) { objectDirectionsXY[determineDirectionsList[i][2]] = -1; }
                else if (WSADUpDownLeftRight[determineDirectionsList[i][1]] == true && WSADUpDownLeftRight[determineDirectionsList[i][0]] == false) { objectDirectionsXY[determineDirectionsList[i][2]] = 1; }
                else { objectDirectionsXY[determineDirectionsList[i][2]] = 0; }
            }

            //COLISIONS! 
            for (int i = 0; i <= 2; i++)
            {
                //IF ANY OBJECTS ARE HITTING THE WALLS; PUSH THEM BACK
                //TOP WALL
                if (movingObjects[i].Y <= 0)
                {
                    if (i == 0)
                    {
                        objectDirectionsXY[0 + 3] *= -1;
                    }
                    canMoveUp[i] = -1;
                    movingObjects[i].Y = 0 + 1;
                }
                //BOTTOM WALL
                if (movingObjects[i].Y >= this.Height - movingObjects[i].Height)
                {
                    if (i == 0) { objectDirectionsXY[0 + 3] *= -1;
                     }
                    canMoveDown[0] = 1;
                    movingObjects[i].Y = this.Height - movingObjects[i].Height - 1;
                }
                //LEFT WALL
                if (movingObjects[i].X < 0)
                {
                    if (i == 0) { objectDirectionsXY[0] *= -1;
                    }
                    canMoveLeft[i] = -1;
                    movingObjects[i].X = 0 + 1;
                 }
                //RIGHT WALL
                if (movingObjects[i].X > this.Width - movingObjects[i].Width)
                {
                    if (i == 0) { objectDirectionsXY[0] *= -1;
                    }

                    canMoveRight[i] = 1;
                    movingObjects[i].X = this.Width - movingObjects[i].Width - 1;
                }

                //OBJECTS INTERACTING WITH EACHOTHER; I wont use .Intersects with because these are circles;
                //--instead I want to compare the position of the ball and the other circles by drawing a line between each circle and
                //--looking at if it is smaller than the sum of their radius's. Math.Sprt((x1-x2)^2 + (y1-y2)^2) <= r1+r2
                if (i != 0 && GetLength(movingObjects[0], movingObjects[i]) <= Convert.ToDouble((movingObjects[0].Width / 2) + (movingObjects[i].Width / 2)))
                {
                    //Puck Right of paddle
                    if (movingObjects[0].X > movingObjects[i].X + movingObjects[0].Width)
                    {
                        objectDirectionsXY[0] = 1;
                        canMoveLeft[0] = -1;
                        canMoveRight[i] = canMoveRight[0];

                        movingObjects[i].X += -2;
                    }

                    //Puck Left of paddle
                    if (movingObjects[0].X < movingObjects[i].X)
                    {
                        objectDirectionsXY[0] = -1;
                        canMoveRight[0] = 1;
                        canMoveLeft[i] = canMoveLeft[0];

                        movingObjects[i].X += 2;
                    }

                    //Puck Below paddle
                    if (movingObjects[0].Y > movingObjects[i].Y + movingObjects[0].Width)
                    {
                        objectDirectionsXY[0 + 3] = 1;
                        canMoveUp[0] = -1;
                        canMoveDown[i] = canMoveDown[0];

                        movingObjects[i].Y += -2;
                    }

                    //Puck Above paddle
                    if (movingObjects[0].Y < movingObjects[i].Y)
                    {
                        objectDirectionsXY[0 + 3] = -1;
                        canMoveDown[0] = 1;
                        canMoveUp[i] = canMoveUp[0];

                        movingObjects[i].Y += 2;
                    }



                    //Apply the velocity of the paddle to the puck when they intersect.
                    objectSpeedsXY[0] = objectSpeedsXY[0] / 2 + paddleVelocitiesXY[i - 1];
                    objectSpeedsXY[0 + 3] = objectSpeedsXY[0 + 3] / 2 + paddleVelocitiesXY[i + 2 - 1];
                    //If the object should not move because the puck is stuck; bounce the object back.
                    }
                //UPDATE OBJECT POSITIONS
                if (objectDirectionsXY[i] != canMoveRight[i] && objectDirectionsXY[i] != canMoveLeft[i])
                {
                    movingObjects[i].X += Convert.ToInt32(objectDirectionsXY[i] * objectSpeedsXY[i]);
                }
                if (objectDirectionsXY[i + 3] != canMoveUp[i] && objectDirectionsXY[i + 3] != canMoveDown[i])
                {
                    movingObjects[i].Y += Convert.ToInt32(objectDirectionsXY[i + 3] * objectSpeedsXY[i + 3]);
                }

                if (GetLength(movingObjects[0], movingObjects[i]) > Convert.ToDouble((movingObjects[0].Width / 2) + (movingObjects[i].Width / 2)) || ( i == 0 && GetLength(movingObjects[0], movingObjects[1]) > Convert.ToDouble((movingObjects[0].Width / 2) + (movingObjects[1].Width / 2)) && GetLength(movingObjects[0], movingObjects[2]) > Convert.ToDouble((movingObjects[0].Width / 2) + (movingObjects[2].Width / 2))))
                {
                    canMoveUp[i] = 0;
                    canMoveDown[i] = 0;
                    canMoveLeft[i] = 0;
                    canMoveRight[i] = 0;
                }

            }



            Refresh();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(redBrush, ScoreZones[0]);
            e.Graphics.FillRectangle(blueBrush, ScoreZones[1]);
            e.Graphics.FillEllipse(redBrush, movingObjects[1]);
            e.Graphics.FillEllipse(blueBrush, movingObjects[2]);
            e.Graphics.FillEllipse(whiteBrush, movingObjects[0]);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            checkKey(true, e);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            checkKey(false, e);
        }


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

        void ResetPositions()
        {
            movingObjects = new Rectangle[] //0 = ball, 1 = player1, 2 = player2
           {
              new Rectangle(this.Width / 2 - diskWidth / 2, this.Height / 2 - diskWidth, diskWidth, diskWidth),
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

    }
}
