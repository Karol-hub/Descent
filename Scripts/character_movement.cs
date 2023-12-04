using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

public partial class character_movement : CharacterBody2D
{
    //Constants
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    //Nodes
    public KinematicCollision2D collision;
    private Sprite2D sprite;
    private Area2D climbCollider;
    //Movement presets
    public Vector2 velocity;
    private Vector2 direction;
    private bool facingLeft;
    public int dashes;
    public int jumps;
    private float coyoteTimer =10f;
    private bool lastFrameGrounded;
    public enum playerState
    {
        idle,
        walking,
        coyote,
        airBorn,
        dashing,
        dashEndLag,
        noResistance
    }
    public playerState state;
    //buffer preset
    public class playerInputBuffer
    {
        public string InputType;
        public float whenPressed;
        public playerInputBuffer(string PlayerIn)
        {
            InputType = PlayerIn;
            whenPressed = 0f;
        }
    }
    public List<playerInputBuffer> InputRegister = new List<playerInputBuffer>();
    public List<playerInputBuffer> MovementInputHistory = new List<playerInputBuffer>();
    // timer
    private float frameDelta;
    private float time;
    //Character presets
    private float inputLifespan =0.2f;
    private float movementInputHistoryLifespan = 0.2f;
    private float decelerateRate = 100f;
    private float noResDecelerateRate = 0.1f;
    private float coyoteWindow =0.1f;
    //moving
    private float speed = 200.0f;
    private float jumpVelocity = -400.0f;
    public int maxJumps = 2;
    //bounce
    private float bounceVel = 40f;
    private float bounceDir = 80f; //in degrees
    private float bounceNoResLength = 0.30f; //in s
    private float strokGravity = 3f; //gravity needs to be stronger cuz it feels cooler
    //dash
    private float dashVelocity = 800f;
    private float dashLength = 0.15f; // in s
    private int endLagLength = 50; //in ms
    private int maxDashes = 2;
   
    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("sprite");
        climbCollider = GetNode<Area2D>("climbBox");
    }
    public override void _Process(double delta)
    {
        frameDelta = (float)delta;
        direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        PlayerInput();
        ManageState();
        ManageSprite();
        Movement();
        lastFrameGrounded = IsOnFloor();
        MannageInputRegister();
        ManageInputHistory();
        Velocity = velocity;
        MoveAndSlide();
    }
    private void Movement()
    {
        #region gravity
        // Add the gravity.
        if (state == playerState.airBorn || state == playerState.coyote)
        {
            velocity.Y += gravity * frameDelta;
        }
        else if (state == playerState.noResistance)
        {
            velocity.Y += gravity * strokGravity * frameDelta;
        }
        #endregion
        #region speed_control
        if (state != playerState.dashing && state != playerState.noResistance)
        {
            velocity.X = (float)Mathf.MoveToward(velocity.X, 0f, decelerateRate) * frameDelta; //the speed doesn't go zooooooooom (but we still like zoom)
        }
        if (IsOnCeiling() && velocity.Y < 0f && state != playerState.dashing)
        {
            velocity.Y = 0f;
        }
        #endregion
        #region move_player
        if (direction != Vector2.Zero && (state != playerState.dashing) && (state != playerState.noResistance))
        {
            velocity.X = direction.X * speed;
        }
        #endregion
        #region jumping
        // Handle Jump.
        if ((InputRegister.Where(x => x.InputType == "jump").Count() != 0 && jumps > 0) && MovementInputHistory.Where(x => x.InputType == "jump").Count() == 0 && (state != playerState.dashing))
        {
            MovementExecute("jump"); //does stuff with the input registers
            jumps -= 1;
            velocity.Y = jumpVelocity;
        }
        #endregion
        #region dash
        if ((InputRegister.Where(x => x.InputType == "dash").Count() != 0) && dashes > 0) //init dash
        {
            MovementExecute("dash");
            state = playerState.dashing;
            dashes -= 1;
            time = 0f;
            if (direction == Vector2.Zero) // If player isn't moving
            {
                if (facingLeft) //dash left
                {
                    velocity -= new Vector2(1f,0f) * dashVelocity;
                }
                else //dash right
                {
                    velocity += new Vector2(1f, 0f) * dashVelocity;
                }
            }
            else // dash in whatever direction facing
            {
                velocity = (direction.Normalized() * dashVelocity);
            }
        }
        if (state == playerState.dashing) //when in dash
        {
            if (climbCollider.HasOverlappingBodies() && InputRegister.Where(x => x.InputType == "jump").Count() != 0) //check for wallbounce
            {
                for (int i = 0; i < climbCollider.GetOverlappingBodies().Count; i++)
                {
                    if (climbCollider.GetOverlappingBodies()[i].GetClass() == "TileMap") //only activate when colliding with walls
                    {
                        MovementExecute("jump");
                        state = playerState.noResistance; //init bounce
                        time = 0f;
                        if (direction == Vector2.Zero)
                        {
                            return;
                        }
                        else if (direction == Vector2.Up) //special wall bounce
                        {
                            velocity += BounceVel(bounceVel, bounceDir);
                        }
                        else if (direction != Vector2.Down) //bounce back from the wall when diagnal or horizontal
                        {
                            dashes = maxDashes; //resets dashes
                            velocity.X *= -1f;
                        }
                        break;
                    }
                }
            }
            if (IsOnFloor() && InputRegister.Where(x => x.InputType == "jump").Count() != 0)//check for floorbounce
            {
                MovementExecute("jump");
                time = 0f;
                state = playerState.noResistance;
                velocity.Y = jumpVelocity;
            }
        }
        #endregion
    }
    private void ManageSprite()
    {
        #region which_dir_facing
        //gets direction of player input
        if (direction.X < 0f)
        {
            facingLeft = true;
        }
        else if (direction.X > 0f)
        {
            facingLeft = false;
        }
        #endregion
        #region sprite dir
        if (facingLeft)
        {
            sprite.Scale = new Vector2(1f, 1f);
            climbCollider.Scale = new Vector2(1f, 1f);
        }
        else
        {
            sprite.Scale = new Vector2(-1f, 1f);
            climbCollider.Scale = new Vector2(-1f, 1f);
        }
        #endregion
    }
    private void ManageState()
    {
        // Handle player state
        #region dash end lag
        if (state == playerState.dashEndLag) //at end of dash
        {
            return;
        }
        #endregion
        #region dash
        else if (state == playerState.dashing) //when in dash
        {
            time += frameDelta;
            if (time >= dashLength)
            {
                EndDash();
                return;
            }
            if (GetSlideCollisionCount() != 0) //end  dash when colliding
            {
                for (int i = 0; i < GetSlideCollisionCount(); i++)
                {
                    if (GetSlideCollision(i).GetCollider().GetClass() == "TileMap" && GetSlideCollision(i).GetAngle()!=0)  
                    {
                        //end dash when colliding with walls but not floor
                        EndDash();
                        return;
                    }
                    //also want something for when you collide with enemy and that would go here
                }
            }
            return;
        }
        #endregion
        #region no resistance
        else if (state == playerState.noResistance) //after bouncing
        {
            if (GetSlideCollisionCount() != 0)
            {
                for (int i = 0; i < GetSlideCollisionCount(); i++)
                {
                    if (GetSlideCollision(i).GetCollider().GetClass() == "TileMap")
                    {
                        //end when colliding with scenery
                        ToIdle();
                        return;
                    }
                }
            }

            time += frameDelta;
            if (time >= bounceNoResLength) //finish no resistance state from bounce
            {
                state = playerState.idle;
            }
            return;
        }
        #endregion
        #region coyote time
        else if ((state == playerState.coyote && coyoteTimer < coyoteWindow &&!IsOnFloor())|| (lastFrameGrounded && !IsOnFloor() && MovementInputHistory.Where(x => x.InputType == "jump").Count() == 0))//coyote time is here cool ig
        {
            state = playerState.coyote;
            coyoteTimer += frameDelta;
            return;
        }
        #endregion
        #region air born
        else if (!IsOnFloor() || (coyoteTimer > coyoteWindow && state == playerState.coyote)) //when not on floor
        {
            if (coyoteTimer > coyoteWindow && state == playerState.coyote)
            {
                coyoteTimer = 0f;
            }
            state = playerState.airBorn;
            return;
        }
        #endregion
        #region idle
        else if (direction == Vector2.Zero && IsOnFloor()) //when idle
        {
            state = playerState.idle;
            dashes = maxDashes; //resets amount of dashes
            jumps = maxJumps; //resets amount of Jumps
            coyoteTimer = 0f; //resets timer for coyote window
            return;
        }
        #endregion
        #region walking
        else if (direction != Vector2.Zero && IsOnFloor()) //when walking
        {
            state = playerState.walking;
            dashes = maxDashes; //resets amount of dashes
            jumps = maxJumps; //resets amount of Jumps
            coyoteTimer = 0f; //resets timer for coyote window
            return;
        }
        #endregion
    }
    private void ManageInputHistory()
    {
        if (MovementInputHistory.Count == 0) //go back if empty
        {
            return;
        }
        for (int i = 0; i < MovementInputHistory.Count; i++) //gets rid of object if it lives too long
        {
            MovementInputHistory[i].whenPressed += frameDelta;
            if (MovementInputHistory[i].whenPressed > movementInputHistoryLifespan)
            {
                MovementInputHistory.RemoveAt(i);
            }
        }
    }
    private void MannageInputRegister()
    { 
        if (InputRegister.Count == 0) //go back if empty
        {
            return;
        }
        for (int i=0;i<InputRegister.Count;i++) //gets rid of object if it lives too long
        {
            InputRegister[i].whenPressed += frameDelta;
            if (InputRegister[i].whenPressed > inputLifespan)
            {
                InputRegister.RemoveAt(i);
            }
        }
    }
    private void PlayerInput() 
    {
        //allows to buffer jumps and dashes
        if (Input.IsActionJustPressed("jump"))
        {
            InputRegister.Add(new playerInputBuffer("jump"));
        }
        else if (Input.IsActionJustPressed("dash"))
        {
            InputRegister.Add(new playerInputBuffer("dash"));
        }
    }
    private void EndDash()
    {
        //Does the stuff that needs to be dont when ending a dash
        state = playerState.dashEndLag;
        velocity = Vector2.Zero;
        Task.Delay(endLagLength).ContinueWith(t => ToIdle());
    }
    private void ToIdle()
    {
        // I mean how hard is it to figure it out cmon man
        state = playerState.idle;
    }
    private Vector2 BounceVel(float mag, float dir)
    {
        // calculates angle for bouncing
        if (facingLeft)
        {
            return new Vector2(-mag * MathF.Sin(dir), -mag * MathF.Cos(dir));
        }
        else
        {
            return new Vector2(mag * MathF.Sin(dir), -mag * MathF.Cos(dir));
        }
    }
    private void MovementExecute(string In)
    {
        // removes the input from input buffer and adds it to history
        MovementInputHistory.Add(new playerInputBuffer(In));
        for (int i = 0; i < InputRegister.Count; i++)
        {
            if (MovementInputHistory[i].InputType == In)
            {
                InputRegister.RemoveAt(i);
                return;
            }
        }
    }
}