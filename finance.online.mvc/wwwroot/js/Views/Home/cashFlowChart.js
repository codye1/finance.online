const numberFmt = new Intl.NumberFormat('uk-UA', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const compactFmt = new Intl.NumberFormat('uk-UA', { notation: 'compact', maximumFractionDigits: 1 });

// Читає CSS-змінну теми. Підтримує і "#16a34a", і shadcn-формат "142 71% 45%".
function token(name, fallback) {
    const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    if (!v) return fallback;
    return /^[\d.]+\s+[\d.]+%\s+[\d.]+%/.test(v) ? `hsl(${v})` : v;
}

function themeColors() {
    return {
        income: token('--income', '#16a34a'),
        expense: token('--expense', '#dc2626'),
        text: token('--muted-foreground', '#6b7280'),
        grid: token('--border', '#e5e7eb')
    };
}

function formatLabel(iso, granularity) {
    const d = new Date(iso);
    return granularity === 'month'
        ? d.toLocaleDateString('uk-UA', { month: 'short', year: '2-digit' })
        : d.toLocaleDateString('uk-UA', { day: '2-digit', month: '2-digit' });
}

function formatTitle(iso, granularity) {
    const d = new Date(iso);
    return granularity === 'month'
        ? d.toLocaleDateString('uk-UA', { month: 'long', year: 'numeric' })
        : d.toLocaleDateString('uk-UA', { day: 'numeric', month: 'long', year: 'numeric' });
}

export function initCashflowChart() {
    const canvas = document.getElementById('cashflow-chart');
    if (!canvas) return;

    if (typeof Chart === 'undefined') {
        console.error('Chart.js не завантажено — перевірте RegisterScript в Index.cshtml.');
        return;
    }

    const series = window.TransitData?.cashflow ?? { granularity: 'day', points: [] };
    const granularity = series.granularity || 'day';
    const points = series.points || [];

    if (points.length === 0) return; // порожній стан показує #cashflow-empty

    const c = themeColors();

    const chart = new Chart(canvas, {
        type: 'bar',
        data: {
            labels: points.map(p => formatLabel(p.date, granularity)),
            datasets: [
                {
                    label: 'Дохід',
                    data: points.map(p => p.income),
                    backgroundColor: c.income,
                    borderRadius: 4,
                    maxBarThickness: 28
                },
                {
                    label: 'Витрати',
                    data: points.map(p => p.expense),
                    backgroundColor: c.expense,
                    borderRadius: 4,
                    maxBarThickness: 28
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        title: items => formatTitle(points[items[0].dataIndex].date, granularity),
                        label: item => `${item.dataset.label}: ${numberFmt.format(item.parsed.y)} грн`,
                        footer: items => {
                            if (items.length < 2) return '';
                            const net = items.reduce(
                                (sum, i) => sum + (i.datasetIndex === 0 ? i.parsed.y : -i.parsed.y), 0);
                            return `Баланс: ${numberFmt.format(net)} грн`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: { color: c.text, maxRotation: 0, autoSkip: true, maxTicksLimit: 10 }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: c.grid },
                    border: { display: false },
                    ticks: { color: c.text, callback: v => compactFmt.format(v) }
                }
            }
        }
    });

    // Фільтр «Усі / Дохід / Витрати»
    $('#chart-filter').on('click', '.filter-toggle', function () {
        const filter = $(this).data('filter');

        $('#chart-filter .filter-toggle').removeClass('active');
        $(this).addClass('active');

        chart.setDatasetVisibility(0, filter !== 'expense');
        chart.setDatasetVisibility(1, filter !== 'income');
        chart.update();
    });

    // Перемальовуємо кольори при зміні теми (клас/атрибут на <html>)
    new MutationObserver(() => {
        const t = themeColors();
        chart.data.datasets[0].backgroundColor = t.income;
        chart.data.datasets[1].backgroundColor = t.expense;
        chart.options.scales.x.ticks.color = t.text;
        chart.options.scales.y.ticks.color = t.text;
        chart.options.scales.y.grid.color = t.grid;
        chart.update('none');
    }).observe(document.documentElement, { attributes: true, attributeFilter: ['class', 'data-theme'] });
}