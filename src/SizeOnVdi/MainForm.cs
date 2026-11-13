using System.Diagnostics;

namespace SizeOnVdi;

public sealed class MainForm : Form
{
    private readonly string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private readonly Label space = new() { AutoSize = true, Text = "Lettura spazio disponibile…", Margin = new Padding(0, 12, 0, 12) };
    private readonly Label status = new() { AutoSize = true, Text = "Pronto", Margin = new Padding(0, 8, 0, 0) };
    private readonly Button refresh = new() { Text = "Analizza di nuovo", AutoSize = true };
    private readonly Button cancel = new() { Text = "Interrompi", AutoSize = true, Enabled = false };
    private readonly Button open = new() { Text = "Apri cartella", AutoSize = true, Enabled = false };
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, RowHeadersVisible = false,
        AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false, BackgroundColor = SystemColors.Window,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
    };
    private CancellationTokenSource? cancellation;

    public MainForm()
    {
        Text = "Spazio profilo • Size on VDI";
        ClientSize = new Size(1000, 620);
        MinimumSize = new Size(700, 450);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "20 file più grandi\n" + profile, AutoSize = true }, 0, 0);
        layout.Controls.Add(space, 0, 1);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        buttons.Controls.AddRange([refresh, cancel, open]);
        layout.Controls.Add(buttons, 0, 2);
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MB", DataPropertyName = "MB", Width = 120,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Percorso completo", DataPropertyName = "FullName",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }, SortMode = DataGridViewColumnSortMode.NotSortable });
        layout.Controls.Add(grid, 0, 3);
        layout.Controls.Add(status, 0, 4);
        Controls.Add(layout);
        refresh.Click += async (_, _) => await Analyze();
        cancel.Click += (_, _) => cancellation?.Cancel();
        open.Click += (_, _) => OpenFolder();
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenFolder(); };
        Shown += async (_, _) => await Analyze();
        FormClosing += (_, _) => cancellation?.Cancel();
    }

    private async Task Analyze()
    {
        if (cancellation != null) return;
        using var cts = new CancellationTokenSource();
        cancellation = cts;
        refresh.Enabled = open.Enabled = false;
        cancel.Enabled = true;
        grid.DataSource = null;
        status.Text = "Analisi in corso…";
        space.Text = "Lettura spazio disponibile…";
        var progress = new Progress<long>(n => { if (!IsDisposed && cancellation == cts && !cts.IsCancellationRequested) status.Text = $"Analisi in corso: {n:N0} file…"; });
        try
        {
            var volumeText = await Task.Run(() =>
            {
                try
                {
                    var v = VolumeReader.Read(profile);
                    return $"{v.Source}\nTotale: {v.Total / 1073741824d:N2} GB     Usato: {(v.Total - v.Free) / 1073741824d:N2} GB     Libero: {v.Free / 1048576d:N2} MB";
                }
                catch (Exception ex) { return "Spazio non disponibile: " + ex.Message; }
            });
            if (IsDisposed) return;
            space.Text = volumeText;
            var result = await Task.Run(() => Scanner.Scan(profile, cts.Token, n => ((IProgress<long>)progress).Report(n)));
            if (IsDisposed) return;
            grid.DataSource = result.Files;
            status.Text = $"Completato • {result.Count:N0} file • {result.Skipped:N0} elementi/cartelle saltati • {DateTime.Now:HH:mm}";
            open.Enabled = result.Files.Count > 0;
        }
        catch (OperationCanceledException) { if (!IsDisposed) status.Text = "Analisi interrotta. Premi Analizza di nuovo per ripartire."; }
        catch (Exception ex) { if (!IsDisposed) status.Text = "Analisi non riuscita: " + ex.Message; }
        finally
        {
            cancellation = null;
            if (!IsDisposed) { refresh.Enabled = true; cancel.Enabled = false; }
        }
    }

    private void OpenFolder()
    {
        if (grid.CurrentRow?.DataBoundItem is not LargeFile file) return;
        try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{file.FullName}\"") { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Impossibile aprire la cartella"); }
    }
}
