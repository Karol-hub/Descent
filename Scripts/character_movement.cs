using Godot;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class character_movement : CharacterBody2D
{
    //Constants
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    //Presets
    public KinematicCollision2D collision;
    public Vector2 velocity;
    private Vector2 direction;
    private Sprite2D sprite;
    private Area2D climbCollider;
    private bool facingLeft;
    public int dashes;
    public int jumps;
    public enum playerState
    {
        idle,
        walking,
        airBorn,
        dashing,
        dashEndLag,
        noResistance
    }
    public playerState state;
    private float frameDelta;
    private float time;
    //Character presets
    private float decelerateRate = 100f;
    private float noResDecelerateRate = 0.1f;
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
        ManageState();
        ManageSprite();
        Movement();
        Velocity = velocity;
        MoveAndSlide();
    }
    private void Movement()
    {
        #region gravity
        // Add the gravity.
        if (state == playerState.airBorn)
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
            velocity.X = (float)Mathf.MoveToward(velocity.X, 0f, decelerateRate) * frameDelta;
        }
        else if (state == playerState.noResistance)
        {
            //velocity.X = (float)Mathf.MoveToward(velocity.X, 0f, noResDecelerateRate) * frameDelta;
        }
        #endregion

        #region move_player
        if (direction != Vector2.Zero && (state != playerState.dashing) && (state != playerState.noResistance))
        {
            velocity.X = direction.X * speed;
        }
        /*
        else if (state == playerState.noResistance)
        {
            velocity.X += direction.X * speed * frameDelta;
        }
        */
        #endregion

        #region jumping
        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && (jumps > 0) && (state != playerState.dashing))
        {
            jumps -= 1;
            velocity.Y = jumpVelocity;
        }
        #endregion

        #region dash
        if (Input.IsActionJustPressed("dash") && (dashes > 0)) //init dash
        {
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
            if (climbCollider.HasOverlappingBodies() && Input.IsActionPressed("jump")) //check for wallbounce
            {
                for (int i = 0; i < climbCollider.GetOverlappingBodies().Count; i++)
                {
                    if (climbCollider.GetOverlappingBodies()[i].GetClass() == "TileMap") //only activate when colliding with walls
                    {
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
            if (IsOnFloor() && Input.IsActionPressed("jump"))//check for floorbounce
            {
                time = 0f;
                state = playerState.noResistance;
                velocity.Y = jumpVelocity;
            }
        }
        #endregion
    }
    private void ManageSprite()
    {
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
    }
    private void ManageState()
    {
        // Handle player state
        if (state == playerState.dashEndLag) //at end of dash
        {
            return;
        }
        else if (state == playerState.dashing) //when in dash
        {
            time += frameDelta;
            if (time >= dashLength)
            {
                EndDash();
                return;
            }
            if (GetSlideCollisionCount() != 0)
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
            if (time >= bounceNoResLength)
            {
                state = playerState.idle;
            }
            return;
        }
        else if (!IsOnFloor()) //when on floor
        {
            state = playerState.airBorn;
            return;
        }
        else if (direction == Vector2.Zero && IsOnFloor()) //when idle
        {
            state = playerState.idle;
            dashes = maxDashes; //resets amount of dashes
            jumps = maxJumps; //resets amount of Jumps
            return;
        }
        else if (direction != Vector2.Zero && IsOnFloor()) //when walking
        {
            state = playerState.walking;
            dashes = maxDashes; //resets amount of dashes
            jumps = maxJumps; //resets amount of Jumps
            return;
        }

    }
    private void EndDash()
    {
        state = playerState.dashEndLag;
        velocity = Vector2.Zero;
        Task.Delay(endLagLength).ContinueWith(t => ToIdle());
    }
    private void ToIdle()
    {
        state = playerState.idle;
    }
    private Vector2 BounceVel(float mag, float dir)
    {
        if (facingLeft)
        {
            return new Vector2(-mag * MathF.Sin(dir), -mag * MathF.Cos(dir));
        }
        else
        {
            return new Vector2(mag * MathF.Sin(dir), -mag * MathF.Cos(dir));
        }
    }
}