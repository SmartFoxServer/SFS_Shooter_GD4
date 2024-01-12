using Godot;
using System;
using System.Diagnostics;

/**
 * ------------------------------------------------------
 * The stat-panel is an included bonus.
 * It will display the Frams per second and the ping rate to the server.
 * It will also display static memory size and 
 * Draw calls, but only when running in the Editor.
 * This should be used for Debuggin and removedfor a commercial game.
 * ------------------------------------------------------
 */
public partial class stats_panel : Control
{
    [ExportCategory("-- Statistics Panel --")]

    [Export] public Label fps;
    [Export] public Label mem;
    [Export] public Label dcs;
    [Export] public Label ping;

    public Control gamemanager;
    private GameManager gm;

    public override void _Ready()
    {
        gamemanager = GetNode<Control>("../../Game");
        gm = (GameManager)gamemanager;

        var monitorValue = new Callable(this, MethodName.GetMonitorValue);
        if(mem != null)
        Performance.AddCustomMonitor("mem", monitorValue);
        if (dcs != null)
            Performance.AddCustomMonitor("dcs", monitorValue);
    }

    public int GetMonitorValue()
    {
        mem.Text = (Math.Round((Performance.GetMonitor(Performance.Monitor.MemoryStatic) /1024f)/1024f)).ToString();
        dcs.Text = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame).ToString();
         return 0;
    }

    public override void _Process(double delta)
    {
        double fpss = Engine.GetFramesPerSecond();
        fps.Text = fpss.ToString();
        double pings = gm.clientServerLag;
        ping.Text = pings.ToString();
    }

    public void exitStats()
    {
        Performance.RemoveCustomMonitor("mem");
        Performance.RemoveCustomMonitor("dcs");
    }
}
