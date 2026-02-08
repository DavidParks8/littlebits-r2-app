using System.Windows.Input;

namespace LittleBitsR2Controller.Views;

/// <summary>
/// Represents a joystick position mapped to R2D2 drive/turn values.
/// Algorithm ported from https://github.com/meetar/littlebits-r2d2-controls
/// </summary>
public class JoystickPosition
{
    /// <summary>Speed from -1.0 (full backward) to 1.0 (full forward), 0 is stop</summary>
    public double Speed { get; init; }

    /// <summary>Turn from -1.0 (full left) to 1.0 (full right), 0 is straight</summary>
    public double Turn { get; init; }
}

/// <summary>
/// A touch-based joystick control that maps position to drive/turn commands.
/// Based on the original joystick algorithm from interface06.html.
/// </summary>
public class JoystickView : GraphicsView, IDrawable
{
    private const double MaxDistance = 64.0;
    private const double Deadzone = 8.0;

    private double _knobX;
    private double _knobY;
    private bool _isDragging;

    public static readonly BindableProperty MovedCommandProperty =
        BindableProperty.Create(nameof(MovedCommand), typeof(ICommand), typeof(JoystickView));

    public static readonly BindableProperty ReleasedCommandProperty =
        BindableProperty.Create(nameof(ReleasedCommand), typeof(ICommand), typeof(JoystickView));

    public ICommand? MovedCommand
    {
        get => (ICommand?)GetValue(MovedCommandProperty);
        set => SetValue(MovedCommandProperty, value);
    }

    public ICommand? ReleasedCommand
    {
        get => (ICommand?)GetValue(ReleasedCommandProperty);
        set => SetValue(ReleasedCommandProperty, value);
    }

    public JoystickView()
    {
        Drawable = this;
        HeightRequest = 200;
        WidthRequest = 200;

        // PanGestureRecognizer tracks the full drag lifecycle and continues
        // tracking even when the finger moves outside the view bounds.
        // TotalX/TotalY are used directly as offset from center, so the
        // knob always starts at center — standard joystick behaviour.
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(pan);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!IsEnabled) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isDragging = true;
                Invalidate();
                break;

            case GestureStatus.Running:
                _isDragging = true; // Fallback in case Started didn't fire on this platform
                var centerX = Width / 2;
                var centerY = Height / 2;
                HandleOffset(e.TotalX, e.TotalY, centerX, centerY);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _isDragging = false;
                _knobX = 0;
                _knobY = 0;
                Invalidate();
                if (ReleasedCommand?.CanExecute(null) == true)
                {
                    ReleasedCommand.Execute(null);
                }
                break;
        }
    }

    private void HandleOffset(double offsetX, double offsetY, double centerX, double centerY)
    {
        // Clamp to max distance (normalized to control size)
        var maxPixelDistance = Math.Min(centerX, centerY) - 20; // 20px padding for knob
        var pixelDistance = Math.Sqrt(offsetX * offsetX + offsetY * offsetY);

        if (pixelDistance > maxPixelDistance)
        {
            var scale = maxPixelDistance / pixelDistance;
            offsetX *= scale;
            offsetY *= scale;
            pixelDistance = maxPixelDistance;
        }

        _knobX = offsetX;
        _knobY = offsetY;
        Invalidate();

        // Map pixel distance to protocol distance (0-64)
        var distance = (pixelDistance / maxPixelDistance) * MaxDistance;

        if (distance < Deadzone)
        {
            // Inside deadzone - no command
            return;
        }

        // Drive angle measured from UP (north), positive clockwise.
        // Matches the original: Math.atan2(xDiff, -yDiff)
        var driveAngle = Math.Atan2(offsetX, -offsetY);

        double speed;
        double turn;

        // 135° limit matches the original's 0.75 * Math.PI
        const double limit = 0.75 * Math.PI;

        if (Math.Abs(driveAngle) < limit)
        {
            // FORWARD zone — turn allowed
            // Speed proportional to distance (positive = forward)
            speed = distance / MaxDistance;
            // Turn mapped from driveAngle: [-limit, limit] → [-1.0, 1.0]
            turn = driveAngle / limit;
        }
        else
        {
            // REVERSE zone — forced straight (matches original: xVal = 17)
            speed = -(distance / MaxDistance);
            turn = 0;
        }

        var position = new JoystickPosition { Speed = speed, Turn = turn };
        if (MovedCommand?.CanExecute(position) == true)
        {
            MovedCommand.Execute(position);
        }
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled))
        {
            if (!IsEnabled)
            {
                _isDragging = false;
                _knobX = 0;
                _knobY = 0;
            }
            Invalidate();
        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var centerX = dirtyRect.Width / 2;
        var centerY = dirtyRect.Height / 2;
        var outerRadius = Math.Min(centerX, centerY) - 5;
        var knobRadius = 30f;
        var disabled = !IsEnabled;

        // Outer ring — subtle border
        canvas.StrokeColor = disabled
            ? Color.FromRgba(130, 130, 130, 35)
            : Color.FromRgba(100, 140, 180, 60);
        canvas.StrokeSize = 2;
        canvas.DrawCircle(centerX, centerY, (float)outerRadius);

        // Base fill — soft translucent dark
        canvas.FillColor = disabled
            ? Color.FromRgba(50, 50, 50, 50)
            : Color.FromRgba(30, 50, 70, 90);
        canvas.FillCircle(centerX, centerY, (float)outerRadius);

        // Inner track ring (where the knob travels)
        var trackRadius = outerRadius - 20;
        canvas.StrokeColor = disabled
            ? Color.FromRgba(130, 130, 130, 25)
            : Color.FromRgba(100, 140, 180, 40);
        canvas.StrokeSize = 1;
        canvas.DrawCircle(centerX, centerY, (float)trackRadius);

        // Crosshairs — thin, subtle
        canvas.StrokeColor = disabled
            ? Color.FromRgba(130, 130, 130, 20)
            : Color.FromRgba(100, 160, 220, 35);
        canvas.StrokeSize = 1;
        canvas.DrawLine(centerX, centerY - (float)outerRadius + 10, centerX, centerY + (float)outerRadius - 10);
        canvas.DrawLine(centerX - (float)outerRadius + 10, centerY, centerX + (float)outerRadius - 10, centerY);

        // Deadzone indicator
        var maxPixelDist = outerRadius - 15;
        var deadzoneRadius = (float)(Deadzone / MaxDistance * maxPixelDist);
        canvas.FillColor = disabled
            ? Color.FromRgba(130, 130, 130, 10)
            : Color.FromRgba(100, 160, 220, 20);
        canvas.FillCircle(centerX, centerY, deadzoneRadius);

        // Direction arrows — small, elegant
        canvas.FontColor = disabled
            ? Color.FromRgba(140, 140, 140, 50)
            : Color.FromRgba(140, 180, 220, 120);
        canvas.FontSize = 14;
        canvas.DrawString("▲", centerX, centerY - (float)outerRadius + 12, HorizontalAlignment.Center);
        canvas.DrawString("▼", centerX, centerY + (float)outerRadius - 22, HorizontalAlignment.Center);
        canvas.DrawString("◀", centerX - (float)outerRadius + 12, centerY - 6, HorizontalAlignment.Center);
        canvas.DrawString("▶", centerX + (float)outerRadius - 12, centerY - 6, HorizontalAlignment.Center);

        // Knob shadow
        var knobPosX = centerX + (float)_knobX;
        var knobPosY = centerY + (float)_knobY;
        canvas.FillColor = Color.FromRgba(0, 0, 0, disabled ? 15 : 40);
        canvas.FillCircle(knobPosX + 2, knobPosY + 2, knobRadius);

        // Knob — blue when active, gray when disabled, neutral at rest
        if (disabled)
        {
            canvas.FillColor = Color.FromRgba(130, 130, 130, 120);
            canvas.FillCircle(knobPosX, knobPosY, knobRadius);
            canvas.StrokeColor = Color.FromRgba(160, 160, 160, 80);
            canvas.StrokeSize = 2;
            canvas.DrawCircle(knobPosX, knobPosY, knobRadius);
            canvas.FillColor = Color.FromRgba(170, 170, 170, 60);
            canvas.FillCircle(knobPosX, knobPosY, 4);
        }
        else if (_isDragging)
        {
            canvas.FillColor = Color.FromRgba(21, 101, 192, 230);  // Primary blue
            canvas.FillCircle(knobPosX, knobPosY, knobRadius);
            canvas.StrokeColor = Color.FromRgba(100, 181, 246, 200);
            canvas.StrokeSize = 2.5f;
            canvas.DrawCircle(knobPosX, knobPosY, knobRadius);
            // Highlight dot in center of knob
            canvas.FillColor = Color.FromRgba(187, 222, 251, 180);
            canvas.FillCircle(knobPosX, knobPosY, 6);
        }
        else
        {
            canvas.FillColor = Color.FromRgba(120, 144, 156, 200);  // Gray-blue
            canvas.FillCircle(knobPosX, knobPosY, knobRadius);
            canvas.StrokeColor = Color.FromRgba(176, 190, 197, 140);
            canvas.StrokeSize = 2;
            canvas.DrawCircle(knobPosX, knobPosY, knobRadius);
            // Subtle center dot
            canvas.FillColor = Color.FromRgba(200, 210, 220, 120);
            canvas.FillCircle(knobPosX, knobPosY, 4);
        }
    }
}
