using Godot;
using System;

public partial class exit : TextureButton
{
    private void _pressed()
    {
        GetTree().Quit();
    }
}
