using System.Threading.Tasks;
using Xunit;

public class TimerTests
{
    [Fact]
    public async Task Single()
    {
        var result = "";

        var t1 = new Timer();
        t1.ScheduleTimer(1, () => result += "pass");
        await t1.WaitForAllPendingTimers();

        Assert.Equal("pass", result);
    }

    [Fact]
    public async Task ScheduleTwoSecondExpiresLater()
    {
        var result = "";

        var t1 = new Timer();
        t1.ScheduleTimer(5, () => result += "pa");
        t1.ScheduleTimer(10, () => result += "ss");
        await t1.WaitForAllPendingTimers();

        Assert.Equal("pass", result);
    }

    [Fact]
    public async Task ScheduleTwoSecondExpiresEarlier()
    {
        var result = "";

        var t1 = new Timer();
        t1.ScheduleTimer(10, () => result += "pa");
        t1.ScheduleTimer(5, () => result += "ss");
        await t1.WaitForAllPendingTimers();

        Assert.Equal("pass", result);
    }

    [Fact]
    public async Task TwoTimersSecondExpiresLater()
    {
        var result = "";

        var t1 = new Timer();
        t1.ScheduleTimer(5, () => result += "pa");
        var t2 = new Timer();
        t2.ScheduleTimer(10, () => result += "ss");
        await t1.WaitForAllPendingTimers();
        await t2.WaitForAllPendingTimers();

        Assert.Equal("pass", result);
    }

    [Fact]
    public async Task TwoTimersSecondExpiresEarlier()
    {
        var result = "";

        var t1 = new Timer();
        t1.ScheduleTimer(10, () => result += "ss");
        var t2 = new Timer();
        t2.ScheduleTimer(5, () => result += "pa");
        await t1.WaitForAllPendingTimers();
        await t2.WaitForAllPendingTimers();

        Assert.Equal("pass", result);
    }

    [Fact]
    public async Task SingleCancel()
    {
        var result = "pass";

        var t1 = new Timer();
        t1.ScheduleTimer(100, () => result += "fail");
        t1.CancelTimer();
        await t1.WaitForAllPendingTimers();

        Assert.Equal("pass", result);
    }

    [Fact]
    public async Task TwoTimersCancelSecond()
    {
        var result = "";

        var t1 = new Timer();
        t1.ScheduleTimer(50, () => result += "pass");
        var t2 = new Timer();
        t2.ScheduleTimer(10, () => result += "fail");
        t2.CancelTimer();
        await t1.WaitForAllPendingTimers();
        await t2.WaitForAllPendingTimers();

        Assert.Equal("pass", result);
    }
}