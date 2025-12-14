using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Factos;

namespace WPFSample.UITests;

public class Tests
{
    [AppTestMethod]
    public async Task Test()
    {
        var sut = new VisualTest.Tabs.View();

        var controller = AppController.Current;

        await controller.NavigateToView(sut);
        await controller.WaitUntilLoaded(sut);

        await Task.Delay(3000);
        sut.tabs.SelectedIndex = 1;
        await Task.Delay(3000);
        sut.tabs.SelectedIndex = 0;
        await Task.Delay(3000);

        var a = 1;
    }
}
