using C4.Modules.Visualization.Application.ExportDiagram;
using C4.Modules.Visualization.Application.Ports;

namespace C4.Modules.Visualization.Tests.Application;

public sealed class ExportDiagramHandlerTests
{
    [Fact]
    public async Task Handle_SvgFormat_ReturnsExportedContentWithSvgContentType()
    {
        var projectId = Guid.NewGuid();
        var expectedBytes = "<svg></svg>"u8.ToArray();
        var readModel = new FakeReadModel("{}");
        var exporter = new FakeExporter("svg", expectedBytes);
        var handler = new ExportDiagramHandler(readModel, [exporter]);

        var result = await handler.Handle(new ExportDiagramCommand(projectId, "svg"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("image/svg+xml");
        result.Value.Content.Should().BeEquivalentTo(expectedBytes);
    }

    [Fact]
    public async Task Handle_PdfFormat_ReturnsPdfContentType()
    {
        var projectId = Guid.NewGuid();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var readModel = new FakeReadModel("{}");
        var exporter = new FakeExporter("pdf", pdfBytes);
        var handler = new ExportDiagramHandler(readModel, [exporter]);

        var result = await handler.Handle(new ExportDiagramCommand(projectId, "pdf"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Handle_DiagramNotFound_ReturnsFailure()
    {
        var readModel = new FakeReadModel(null);
        var exporter = new FakeExporter("svg", []);
        var handler = new ExportDiagramHandler(readModel, [exporter]);

        var result = await handler.Handle(new ExportDiagramCommand(Guid.NewGuid(), "svg"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("visualization.diagram.not_found");
    }

    [Fact]
    public async Task Handle_UnsupportedFormat_ReturnsFailure()
    {
        var projectId = Guid.NewGuid();
        var readModel = new FakeReadModel("{}");
        var svgExporter = new FakeExporter("svg", []);
        var handler = new ExportDiagramHandler(readModel, [svgExporter]);

        var result = await handler.Handle(new ExportDiagramCommand(projectId, "bmp"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("visualization.export.unsupported");
    }

    [Fact]
    public async Task Handle_FormatMatchingIsCaseInsensitive()
    {
        var projectId = Guid.NewGuid();
        var readModel = new FakeReadModel("{}");
        var exporter = new FakeExporter("svg", "<svg/>"u8.ToArray());
        var handler = new ExportDiagramHandler(readModel, [exporter]);

        var result = await handler.Handle(new ExportDiagramCommand(projectId, "SVG"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    private sealed class FakeReadModel(string? diagramJson) : IDiagramReadModel
    {
        public Task<string?> GetDiagramJsonAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(diagramJson);
    }

    private sealed class FakeExporter(string format, byte[] content) : IDiagramExporter
    {
        public string Format => format;

        public Task<byte[]> ExportAsync(string diagramJson, CancellationToken cancellationToken)
            => Task.FromResult(content);
    }
}
