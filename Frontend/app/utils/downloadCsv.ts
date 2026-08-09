function escapeCsvField(value: string): string {
    if (/[",\n\r]/.test(value)) return `"${value.replaceAll('"', '""')}"`;
    return value;
}

export function downloadCsv(filename: string, headers: string[], rows: string[][]) {
    const lines = [headers, ...rows].map((row) => row.map(escapeCsvField).join(','));
    const blob = new Blob([lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
}
