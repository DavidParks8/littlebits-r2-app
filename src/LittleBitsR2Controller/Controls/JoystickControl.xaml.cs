using Microsoft.Maui.Controls.Shapes;

namespace LittleBitsR2Controller.Controls;

public partial class JoystickControl : ContentView
{
    // Maximum distance the joystick can move from center (in pixels)
    private const double MaxDistance = 64.0;
    
    // Deadzone - minimum distance before joystick registers movement
    private const double Deadzone = 8.0;
    
    // Threshold angle for forward vs reverse (in degrees)
    private const double AngleLimit = 45.0;
    
    // Touch tracking
    private bool _isDragging;
    private Point _lastTouchPoint;
    
    // Bindable properties for drive and turn values
    public static readonly BindableProperty DriveValueProperty =
        BindableProperty.Create(nameof(DriveValue), typeof(double), typeof(JoystickControl), 0.0);
    
    public static readonly BindableProperty TurnValueProperty =
        BindableProperty.Create(nameof(TurnValue), typeof(double), typeof(JoystickControl), 0.0);
    
    public double DriveValue
    {
        get => (double)GetValue(DriveValueProperty);
        set => SetValue(DriveValueProperty, value);
    }
    
    public double TurnValue
    {
        get => (double)GetValue(TurnValueProperty);
        set => SetValue(TurnValueProperty, value);
    }
    
    // Event to notify when joystick position changes
    public event EventHandler<JoystickEventArgs>? PositionChanged;
    
    // Event to notify when joystick is released
    public event EventHandler? Released;

    public JoystickControl()
    {
        InitializeComponent();
        
        // Add pan gesture recognizer for drag interaction
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isDragging = true;
                _lastTouchPoint = new Point(e.TotalX, e.TotalY);
                break;
                
            case GestureStatus.Running:
                if (_isDragging)
                {
                    UpdateJoystickPosition(e.TotalX, e.TotalY);
                }
                break;
                
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _isDragging = false;
                ResetJoystick();
                break;
        }
    }

    private void UpdateJoystickPosition(double offsetX, double offsetY)
    {
        // Calculate distance from center
        var distance = Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
        
        // Check if within deadzone
        if (distance < Deadzone)
        {
            ResetJoystick();
            return;
        }
        
        // Clamp to max distance
        if (distance > MaxDistance)
        {
            var angle = Math.Atan2(offsetY, offsetX);
            offsetX = MaxDistance * Math.Cos(angle);
            offsetY = MaxDistance * Math.Sin(angle);
            distance = MaxDistance;
        }
        
        // Update visual position
        JoystickThumb.TranslationX = offsetX;
        JoystickThumb.TranslationY = offsetY;
        
        // Calculate angle in degrees
        var angleRadians = Math.Atan2(offsetY, offsetX);
        var angleDegrees = angleRadians * 180.0 / Math.PI;
        
        double driveIndex;
        double turnIndex;
        
        // Calculate drive and turn indices based on original algorithm
        if (Math.Abs(angleDegrees) < AngleLimit)
        {
            // FORWARD MOVEMENT
            // Turn: map angle from [-45, +45] to [2, 33] -> normalized to [-1, 1]
            var xVal = ((33.0 - 2.0) * (angleDegrees + AngleLimit)) / (2.0 * AngleLimit) + 2.0;
            turnIndex = xVal;
            
            // Drive: map distance from [0, 64] to [31, 0] (stop to full forward) -> normalized to [0, 1]
            var yVal = ((0.0 - 31.0) * distance) / 64.0 + 31.0;
            driveIndex = yVal;
        }
        else
        {
            // REVERSE MOVEMENT
            // Turn: straight ahead
            turnIndex = 17.0; // Index 15-17 is approximately straight
            
            // Drive: map distance from [0, 64] to [31, 62] (stop to full reverse) -> normalized to [-1, 0]
            var yVal = ((62.0 - 31.0) * distance) / 64.0 + 31.0;
            driveIndex = yVal;
        }
        
        // Convert indices to normalized values (-1.0 to 1.0 for both)
        // Drive: 0 = full forward (1.0), 31 = stop (0.0), 62 = full reverse (-1.0)
        var normalizedDrive = (31.0 - driveIndex) / 31.0;
        
        // Turn: 1 = full left (-1.0), 17 = straight (0.0), 33 = full right (1.0)
        var normalizedTurn = (turnIndex - 17.0) / 16.0;
        
        // Clamp values
        normalizedDrive = Math.Clamp(normalizedDrive, -1.0, 1.0);
        normalizedTurn = Math.Clamp(normalizedTurn, -1.0, 1.0);
        
        // Update bindable properties
        DriveValue = normalizedDrive;
        TurnValue = normalizedTurn;
        
        // Raise position changed event
        PositionChanged?.Invoke(this, new JoystickEventArgs(normalizedDrive, normalizedTurn));
    }

    private void ResetJoystick()
    {
        // Animate back to center
        var animation = new Animation(
            v => JoystickThumb.TranslationX = v,
            JoystickThumb.TranslationX,
            0);
        
        var animation2 = new Animation(
            v => JoystickThumb.TranslationY = v,
            JoystickThumb.TranslationY,
            0);
        
        animation.Commit(this, "ResetX", length: 200, easing: Easing.CubicOut);
        animation2.Commit(this, "ResetY", length: 200, easing: Easing.CubicOut);
        
        // Reset values
        DriveValue = 0.0;
        TurnValue = 0.0;
        
        // Raise released event
        Released?.Invoke(this, EventArgs.Empty);
    }
}

public class JoystickEventArgs : EventArgs
{
    public double Drive { get; }
    public double Turn { get; }
    
    public JoystickEventArgs(double drive, double turn)
    {
        Drive = drive;
        Turn = turn;
    }
}
