import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { renderToString } from 'react-dom/server';
import { useDiagramExport } from './useDiagramExport';
import { DiagramData } from '../types';

const SAMPLE_DATA: DiagramData = {
  nodes: [
    { id: 'a', label: 'Service A', level: 'Container', health: 'green', serviceType: 'api', position: { x: 0, y: 0 } },
    { id: 'b', label: 'Service B', level: 'Container', health: 'yellow', serviceType: 'database', position: { x: 300, y: 0 } },
    { id: 'c', label: 'Service C', level: 'Container', health: 'red', serviceType: 'cache', position: { x: 600, y: 0 } },
  ],
  edges: [
    { id: 'e1', from: 'a', to: 'b', traffic: 0.9 },
    { id: 'e2', from: 'b', to: 'c', traffic: 0.4 },
  ],
};

const EMPTY_DATA: DiagramData = { nodes: [], edges: [] };

const DATA_WITH_DRIFT: DiagramData = {
  nodes: [
    { id: 'd1', label: 'Drifted Service', level: 'Container', health: 'yellow', drift: true, serviceType: 'cache', position: { x: 0, y: 0 } },
  ],
  edges: [],
};

const DATA_WITH_SPECIAL_CHARS: DiagramData = {
  nodes: [
    { id: 's1', label: 'Service <A> & "B"', level: 'Container', health: 'green', serviceType: 'app', position: { x: 0, y: 0 } },
  ],
  edges: [],
};

function SvgContentHarness({ data }: { data: DiagramData }) {
  const { exportAs } = useDiagramExport(data);
  return <output data-has-export={typeof exportAs === 'function' ? 'true' : 'false'} />;
}

function captureExportFn(data: DiagramData): (format: 'svg' | 'png' | 'pdf' | 'graphml') => Promise<void> {
  let exportFn: ((format: 'svg' | 'png' | 'pdf' | 'graphml') => Promise<void>) | undefined;

  function CaptureHarness() {
    const { exportAs } = useDiagramExport(data);
    exportFn = exportAs;
    return <output />;
  }

  renderToString(<CaptureHarness />);

  if (!exportFn) throw new Error('exportAs was not captured');
  return exportFn;
}

describe('useDiagramExport', () => {
  it('returns an exportAs function', () => {
    const rendered = renderToString(<SvgContentHarness data={SAMPLE_DATA} />);
    expect(rendered).toContain('data-has-export="true"');
  });

  it('provides exportAs for svg format', () => {
    const rendered = renderToString(<SvgContentHarness data={SAMPLE_DATA} />);
    expect(rendered).toContain('data-has-export="true"');
  });

  it('provides exportAs for pdf format', () => {
    const rendered = renderToString(<SvgContentHarness data={EMPTY_DATA} />);
    expect(rendered).toContain('data-has-export="true"');
  });
});

describe('useDiagramExport SVG generation', () => {
  let capturedDownloadName: string;
  let capturedSvgStrings: string[];
  let clickSpy: ReturnType<typeof vi.fn>;
  let originalCreateElement: typeof document.createElement;
  let OriginalBlob: typeof Blob;

  beforeEach(() => {
    capturedDownloadName = '';
    capturedSvgStrings = [];
    clickSpy = vi.fn();

    originalCreateElement = document.createElement.bind(document);
    OriginalBlob = globalThis.Blob;

    document.createElement = ((tagName: string, options?: ElementCreationOptions) => {
      if (tagName === 'a') {
        return {
          set href(_val: string) { /* noop */ },
          get href() { return ''; },
          set download(val: string) { capturedDownloadName = val; },
          get download() { return capturedDownloadName; },
          click: clickSpy,
        } as unknown as HTMLAnchorElement;
      }
      return originalCreateElement(tagName, options);
    }) as typeof document.createElement;

    globalThis.Blob = class extends OriginalBlob {
      constructor(parts?: BlobPart[], options?: BlobPropertyBag) {
        super(parts, options);
        if (parts) {
          const text = parts.map((p) => (typeof p === 'string' ? p : '')).join('');
          if (text.length > 0) {
            capturedSvgStrings.push(text);
          }
        }
      }
    } as typeof Blob;

    URL.createObjectURL = () => 'blob:mock-url';
    URL.revokeObjectURL = () => {};
  });

  afterEach(() => {
    document.createElement = originalCreateElement;
    globalThis.Blob = OriginalBlob;
  });

  it('triggers SVG download with correct filename via exportAs', async () => {
    const exportFn = captureExportFn(SAMPLE_DATA);
    await exportFn('svg');

    expect(capturedSvgStrings.length).toBe(1);
    expect(clickSpy).toHaveBeenCalledTimes(1);
    expect(capturedDownloadName).toBe('architecture-diagram.svg');
  });

  it('generates valid SVG content with node labels', async () => {
    const exportFn = captureExportFn(SAMPLE_DATA);
    await exportFn('svg');

    expect(capturedSvgStrings.length).toBe(1);
    const svgText = capturedSvgStrings[0]!;

    expect(svgText).toContain('<svg');
    expect(svgText).toContain('Service A');
    expect(svgText).toContain('Service B');
    expect(svgText).toContain('Service C');
  });

  it('includes edge traffic percentages in SVG', async () => {
    const exportFn = captureExportFn(SAMPLE_DATA);
    await exportFn('svg');

    const svgText = capturedSvgStrings[0]!;
    expect(svgText).toContain('90%');
    expect(svgText).toContain('40%');
  });

  it('escapes special XML characters in labels', async () => {
    const exportFn = captureExportFn(DATA_WITH_SPECIAL_CHARS);
    await exportFn('svg');

    const svgText = capturedSvgStrings[0]!;
    expect(svgText).toContain('&lt;A&gt;');
    expect(svgText).toContain('&amp;');
    expect(svgText).toContain('&quot;B&quot;');
  });

  it('includes drift indicator for drifted nodes', async () => {
    const exportFn = captureExportFn(DATA_WITH_DRIFT);
    await exportFn('svg');

    const svgText = capturedSvgStrings[0]!;
    expect(svgText).toContain('DRIFT');
  });

  it('uses Dagre-computed positions from data', async () => {
    const dataWithPositions: DiagramData = {
      nodes: [
        { id: 'p1', label: 'Positioned', level: 'Container', health: 'green', serviceType: 'api', position: { x: 150, y: 200 } },
      ],
      edges: [],
    };

    const exportFn = captureExportFn(dataWithPositions);
    await exportFn('svg');

    const svgText = capturedSvgStrings[0]!;
    expect(svgText).toContain('translate(230,280)');
  });

  it('creates SVG blob and Image for PNG export', async () => {
    let capturedSrc = '';
    const OriginalImage = globalThis.Image;
    globalThis.Image = class MockImage {
      onload: (() => void) | null = null;
      onerror: (() => void) | null = null;
      naturalWidth = 400;
      naturalHeight = 300;
      set src(val: string) {
        capturedSrc = val;
      }
      get src() {
        return capturedSrc;
      }
    } as unknown as typeof Image;

    const exportFn = captureExportFn(SAMPLE_DATA);
    await exportFn('png');

    expect(capturedSvgStrings.length).toBeGreaterThanOrEqual(1);
    const svgContent = capturedSvgStrings.find(s => s.includes('<svg'));
    expect(svgContent).toBeDefined();
    expect(capturedSrc).toBe('blob:mock-url');

    globalThis.Image = OriginalImage;
  });
});
