using Godot;
using System;

public partial class options_back : TextureButton
{
    bool move = false;
    float progress = 0;
    float length = 1;
    Node2D cam;
    Vector2 startPos = new Vector2(0f, 350f);
    Vector2 endPos = new Vector2(0f, 0f);
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        cam = GetNode<Node2D>("/root/main_menue/camHold");
    }
    private void _pressed()
    {
        move = true;
        progress = 0;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (move)
        {
            progress += (float)delta;
            cam.Position = startPos + (Ease(progress / length) * (endPos - startPos));
            if (progress > length)
            {
                move = false;
                progress = 0;
                cam.Position = endPos;
            }
        }
    }

    private float Ease(float x)
    {
        float c4 = (2f * Mathf.Pi) / 3f;
        return (float)(Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10 - 0.75) * c4) + 1f);
    }
}