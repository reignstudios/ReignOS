namespace ReignOS.Service;

public struct ButtonEvent
{
    public bool on, down, up;

    public void Update(bool pressed)
    {
        down = false;
        up = false;
        if (pressed)
        {
            if (!on) down = true;
        }
        else
        {
            if (on) up = true;
        }
        on = pressed;
    }
}