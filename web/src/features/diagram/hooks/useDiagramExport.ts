import { useCallback } from 'react';
import { fetchBlob, ApiError } from '../../../shared/api/client';
import { DiagramData } from '../types';
import { healthColor, trafficColor } from '../utils';

const NODE_WIDTH = 220;
const NODE_HEIGHT = 80;
const PADDING = 80;

function escapeXml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

function buildSvg(data: DiagramData): string {
  const positioned = data.nodes.map((node) => ({
    ...node,
    x: (node.position?.x ?? 0) + PADDING,
    y: (node.position?.y ?? 0) + PADDING,
  }));

  const maxX = positioned.reduce((max, n) => Math.max(max, n.x + NODE_WIDTH), 0);
  const maxY = positioned.reduce((max, n) => Math.max(max, n.y + NODE_HEIGHT), 0);
  const width = maxX + PADDING;
  const height = maxY + PADDING;

  const positionById = new Map(positioned.map((n) => [n.id, n]));

  const edgesSvg = data.edges
    .map((edge) => {
      const src = positionById.get(edge.from);
      const dst = positionById.get(edge.to);
      if (!src || !dst) return '';
      const x1 = src.x + NODE_WIDTH / 2;
      const y1 = src.y + NODE_HEIGHT;
      const x2 = dst.x + NODE_WIDTH / 2;
      const y2 = dst.y;
      const color = trafficColor(edge.traffic, edge.trafficState);
      const mx = (x1 + x2) / 2;
      const my = (y1 + y2) / 2;
      return `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${color}" stroke-width="2" marker-end="url(#arrow-${color.slice(1)})"/>
<text x="${mx}" y="${my - 4}" font-family="Inter,sans-serif" font-size="10" fill="${color}" text-anchor="middle">${Math.round(edge.traffic * 100)}%</text>`;
    })
    .join('\n');

  const nodesSvg = positioned
    .map((node) => {
      const nodeColor = node.telemetryStatus === 'known' ? healthColor(node.health) : '#64748b';
      return `<g transform="translate(${node.x},${node.y})">
  <rect width="${NODE_WIDTH}" height="${NODE_HEIGHT}" rx="8" ry="8" fill="#1a2744" stroke="${nodeColor}" stroke-width="2"/>
  <text x="12" y="26" font-family="Inter,sans-serif" font-size="13" font-weight="600" fill="#e0e6f0">${escapeXml(node.label)}</text>
  <text x="12" y="44" font-family="Inter,sans-serif" font-size="11" fill="#8896b3">${escapeXml(node.level)} · ${escapeXml(node.serviceType)}</text>
  <text x="${NODE_WIDTH - 12}" y="26" font-family="Inter,sans-serif" font-size="10" fill="${nodeColor}" text-anchor="end">${node.telemetryStatus === 'known' ? node.health.toUpperCase() : 'UNKNOWN'}</text>
  ${node.drift ? `<text x="12" y="66" font-family="Inter,sans-serif" font-size="10" fill="#f4a261">DRIFT</text>` : ''}
</g>`;
    })
    .join('\n');

  const arrowColors = ['2e8f5e', '9d7c35', '9e3a3a', '64748b'];
  const defs = arrowColors
    .map(
      (c) =>
        `<marker id="arrow-${c}" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
  <path d="M0,0 L0,6 L8,3 z" fill="#${c}"/>
</marker>`,
    )
    .join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
<defs>${defs}</defs>
<rect width="${width}" height="${height}" fill="#0f1b2d"/>
${edgesSvg}
${nodesSvg}
</svg>`;
}

function buildGraphMl(data: DiagramData): string {
  const nodes = data.nodes
    .map((node) => `<node id="${escapeXml(node.id)}"><data key="label">${escapeXml(node.label)}</data><data key="level">${escapeXml(node.level)}</data></node>`)
    .join('\n');
  const edges = data.edges
    .map((edge) => `<edge id="${escapeXml(edge.id)}" source="${escapeXml(edge.from)}" target="${escapeXml(edge.to)}" />`)
    .join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>
<graphml xmlns="http://graphml.graphdrawing.org/xmlns">
  <key id="label" for="node" attr.name="label" attr.type="string" />
  <key id="level" for="node" attr.name="level" attr.type="string" />
  <graph id="G" edgedefault="directed">
    ${nodes}
    ${edges}
  </graph>
</graphml>`;
}

function buildSimplePdfText(title: string, lines: string[]): Blob {
  const safeLines = [title, ...lines].slice(0, 40).map((line) => line.replace(/[()\\]/g, '\\$&'));
  const content = `BT\n/F1 11 Tf\n50 780 Td\n${safeLines.map((line, idx) => `${idx > 0 ? 'T*\n' : ''}(${line}) Tj`).join('\n')}\nET\n`;
  const bytes = new TextEncoder().encode(content);

  const objects = [
    '1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n',
    '2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n',
    '3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj\n',
    '4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n',
    `5 0 obj << /Length ${bytes.length} >> stream\n${content}endstream\nendobj\n`,
  ];

  let pdf = '%PDF-1.4\n';
  const offsets = [0];
  for (const obj of objects) {
    offsets.push(pdf.length);
    pdf += obj;
  }

  const xrefOffset = pdf.length;
  pdf += `xref\n0 ${objects.length + 1}\n`;
  pdf += '0000000000 65535 f \n';
  for (let i = 1; i <= objects.length; i++) {
    pdf += `${offsets[i]!.toString().padStart(10, '0')} 00000 n \n`;
  }
  pdf += 'trailer << /Size 6 /Root 1 0 R >>\n';
  pdf += 'startxref\n';
  pdf += `${xrefOffset}\n`;
  pdf += '%%EOF';

  return new Blob([pdf], { type: 'application/pdf' });
}

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

function isApiError(value: unknown): value is ApiError {
  return value instanceof ApiError;
}

function downloadLocalSvg(svg: string) {
  const blob = new Blob([svg], { type: 'image/svg+xml' });
  downloadBlob(blob, 'architecture-diagram.svg');
}

function downloadLocalPng(svg: string) {
  const svgBlob = new Blob([svg], { type: 'image/svg+xml' });
  const svgUrl = URL.createObjectURL(svgBlob);
  const img = new Image();
  img.onload = () => {
    const canvas = document.createElement('canvas');
    canvas.width = img.naturalWidth * 2;
    canvas.height = img.naturalHeight * 2;
    const ctx = canvas.getContext('2d');
    if (!ctx) {
      URL.revokeObjectURL(svgUrl);
      return;
    }
    ctx.scale(2, 2);
    ctx.drawImage(img, 0, 0);
    URL.revokeObjectURL(svgUrl);
    canvas.toBlob((pngBlob) => {
      if (pngBlob) {
        downloadBlob(pngBlob, 'architecture-diagram.png');
      }
    }, 'image/png');
  };
  img.onerror = () => {
    URL.revokeObjectURL(svgUrl);
    downloadLocalSvg(svg);
  };
  img.src = svgUrl;
}

export function useDiagramExport(data: DiagramData, projectId?: string) {
  const exportAs = useCallback(
    async (format: 'svg' | 'png' | 'pdf' | 'graphml') => {
      if (projectId !== undefined) {
        try {
          const blob = await fetchBlob(
            `/api/projects/${projectId}/diagram/export?format=${format}`,
          );
          downloadBlob(blob, `architecture-diagram.${format}`);
          return;
        } catch (err: unknown) {
          if (isApiError(err)) {
            console.warn(`Backend export failed (${err.status}), falling back to local export`);
          }
        }
      }

      const svg = buildSvg(data);
      if (format === 'svg') {
        downloadLocalSvg(svg);
        return;
      }

      if (format === 'png') {
        downloadLocalPng(svg);
        return;
      }

      if (format === 'graphml') {
        downloadBlob(new Blob([buildGraphMl(data)], { type: 'application/graphml+xml' }), 'architecture-diagram.graphml');
        return;
      }

      const lines = [
        `Nodes: ${data.nodes.length}`,
        `Edges: ${data.edges.length}`,
        ...data.nodes.slice(0, 20).map((node) => `- ${node.label} [${node.level}]`),
      ];
      downloadBlob(buildSimplePdfText('C4 Diagram Export', lines), 'architecture-diagram.pdf');
    },
    [data, projectId],
  );

  return { exportAs };
}
