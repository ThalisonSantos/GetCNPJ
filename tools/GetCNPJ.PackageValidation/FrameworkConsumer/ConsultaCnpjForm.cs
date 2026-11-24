using System;
using System.Threading;
using System.Windows.Forms;
using GetCNPJ;
using GetCNPJ.Exceptions;

namespace PackageValidation
{
    // Exemplo compilado contra o nupkg. Não é fonte do SysEmpresa.
    public sealed class ConsultaCnpjForm : Form
    {
        private readonly CnpjClient _client;
        private readonly TextBox _cnpj = new TextBox { Width = 200 };
        private readonly Button _consultar = new Button { Text = "Consultar" };
        private readonly Button _cancelar = new Button { Text = "Cancelar" };
        private readonly Label _status = new Label { AutoSize = true };
        private CancellationTokenSource? _pending;
        private bool _closing;

        // Cliente compartilhado da aplicação, emprestado; o formulário não o descarta.
        public ConsultaCnpjForm(CnpjClient client)
        {
            _client = client;
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            panel.Controls.AddRange(new Control[] { _cnpj, _consultar, _cancelar, _status });
            Controls.Add(panel);
            _consultar.Click += ConsultarClick;
            _cancelar.Click += (_, _) => _pending?.Cancel();
        }

        private async void ConsultarClick(object? sender, EventArgs e)
        {
            if (_pending != null) return;
            using var cancellation = new CancellationTokenSource();
            _pending = cancellation;
            _consultar.Enabled = false;
            try
            {
                // Sem ConfigureAwait(false) aqui: a continuação atualiza controles WinForms.
                var result = await _client.GetAsync(_cnpj.Text, cancellation.Token);
                if (_closing || IsDisposed) return;
                if (!result.Success || result.Data == null)
                {
                    _status.Text = "Consulta indisponível; confira o cadastro manualmente.";
                    return;
                }
                var draft = new CustomerDraft(result.Data, result.Data.Endereco?.Uf ?? "");
                _status.Text = draft.RazaoSocial ?? "Razão social não informada";
                // Exibir o rascunho e a lista de IEs para confirmação antes de gravar no cadastro.
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                if (!_closing && !IsDisposed) _status.Text = "Consulta cancelada.";
            }
            catch (InvalidCnpjException)
            {
                if (!_closing && !IsDisposed) _status.Text = "CNPJ inválido.";
            }
            catch (Exception)
            {
                // ErrorMessage/Exception.Message podem conter corpo HTTP e dados pessoais.
                if (!_closing && !IsDisposed) _status.Text = "Não foi possível concluir a consulta.";
            }
            finally
            {
                _pending = null;
                if (!_closing && !IsDisposed) _consultar.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel) { _closing = true; _pending?.Cancel(); }
        }
    }
}
