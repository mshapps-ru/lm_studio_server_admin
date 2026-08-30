// Models.js – client side logic for Models tab
const Models = {
    async init() {
        // Fetch and render models when the tab becomes active
        const tabBtn = document.querySelector('button[data-tab="models"]');
        if (!tabBtn) return;
        tabBtn.addEventListener('click', () => this.loadAndRender());
        // Also load immediately if page loads on Models tab (rare)
        if (document.getElementById('models-tab').classList.contains('active')) {
            await this.loadAndRender();
        }
    },
    async loadAndRender() {
        const updateBtn = document.getElementById('update-list-btn');
        if (!updateBtn) {
            const btn = document.createElement('button');
            btn.id = 'update-list-btn';
            btn.textContent = 'Update list';
            btn.className = 'btn btn-primary';
            btn.addEventListener('click', () => this.loadAndRender());
            const container = document.getElementById('models-tab');
            if (container) {
                container.prepend(btn);
            }
        }
        try {
            const data = await App.apiFetch('/api/models');
            if (data && data.models) {
                this.render(data.models);
            }
        } catch (e) {
            console.error('Failed to load models', e);
        }
    },
    render(models) {
        const container = document.getElementById('models-tab');
        if (!container) return;
        // Preserve existing button
        let updateBtn = container.querySelector('#update-list-btn');
        if (!updateBtn) {
            updateBtn = document.createElement('button');
            updateBtn.id = 'update-list-btn';
            updateBtn.textContent = 'Update list';
            updateBtn.className = 'btn btn-primary';
            updateBtn.addEventListener('click', () => this.loadAndRender());
            container.appendChild(updateBtn);
        }

        // Clear only table area
        const existingTable = container.querySelector('.models-table');
        if (existingTable) existingTable.remove();

        const table = document.createElement('table');
        table.className = 'models-table';
        // Header
        const header = document.createElement('tr');
        ['ID', 'Object', 'Owned_by'].forEach(txt => {
            const th = document.createElement('th');
            th.textContent = txt;
            header.appendChild(th);
        });
        const paramTh = document.createElement('th');
        paramTh.textContent = 'Parameters';
        header.appendChild(paramTh);
        table.appendChild(header);

        models.forEach((m,i) => {
            const row = document.createElement('tr');
            [m.Id, m.Object, m.Owned_by].forEach(val => {
                const td = document.createElement('td');
                td.textContent = val;
                row.appendChild(td);
            });
            // Parameters placeholder
            const paramTd = document.createElement('td');
            const pre = document.createElement('pre');
            pre.textContent = JSON.stringify(m.Parameters || {}, null, 2);
            paramTd.appendChild(pre);
            row.appendChild(paramTd);
            table.appendChild(row);
            // Add separator between rows (except after last)
            if (i < models.length - 1) {
                const sepRow = document.createElement('tr');
                const sepCell = document.createElement('td');
                sepCell.colSpan = 4;
                const hr = document.createElement('hr');
                hr.style.margin = '0';
                sepCell.appendChild(hr);
                sepRow.appendChild(sepCell);
                table.appendChild(sepRow);
            }
        });
        container.appendChild(table);
    }
};
