using KEDA_EdgeServices.Models;
using KEDA_EdgeServices.Protocols.Interfaces;
using Newtonsoft.Json.Linq;
using System.Drawing.Printing;
using System.Xml.Linq;
using HslDesign;
using System.Drawing;
using KEDA_EdgeServices.Protocols.Attributes;

namespace KEDA_EdgeServices.Protocols.Tcp;

[ProtocolType("GP1125T")]
public class GP1125TAdater : IProtocolAdapter
{
    public async Task<List<DeviceDataResult>> ReadOrWriteAsync(Protocol protocol, CancellationToken ct)
    {
        var xmlPath = @"D:\ThreeCode\PrintTemplate.xml";
        HslDesignCore designCore = new(XElement.Load(xmlPath));


        var values = new Dictionary<string, object>();

        foreach(var point in protocol.Devices[0].Points)
        {
            values[point.Label] = point.Address;
        }

        designCore.SetDictionaryValues(values);

        PrintDocument print = new PrintDocument();
        print.PrintPage += (object sender, PrintPageEventArgs e) =>
        {

            PaintResource paintResource = new PaintResource();
            paintResource.Width = designCore.DesignWidth;
            paintResource.Height = designCore.DesignHeight;
            paintResource.DefaultFont = new Font("微软雅黑", 9f);
            paintResource.G = e.Graphics;

            // 如果需要平滑绘制，可以写下面两行代码
            // paintResource.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // paintResource.G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            designCore.DrawDesign(paintResource);
        };
        print.PrinterSettings.PrinterName = protocol.Devices[0].EquipmentID;

        print.Print();

        await Task.CompletedTask;

        return [new DeviceDataResult {
            WriteDeviceStatus = Enums.DeviceWriteStatus.AllPointsWriteSuccess,
        }];
    }
}
