using System;
using System.Threading;
using System.Windows.Forms;

namespace PackageValidation
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            // Sem janela visível: testa controles e message loop WinForms reais.
            using var context = new ApplicationContext();
            using var form = new Form();
            using var label = new Label();
            form.Controls.Add(label);
            var handle = form.Handle;
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            var thread = Thread.CurrentThread.ManagedThreadId;
            var exit = 1;
            form.BeginInvoke(new Action(async () =>
            {
                try
                {
                    await ConsumerChecks.RunAsync();
                    await ConsumerChecks.UiQueryAsync();
                    ConsumerChecks.Require(Thread.CurrentThread.ManagedThreadId == thread && !form.InvokeRequired, "Retorno ao contexto UI");
                    label.Text = "Dados sintéticos recebidos na UI";
                    ConsumerChecks.Require(label.Text.Length > 0, "Atualização de controle");
                    Console.WriteLine("APROVADO: WinForms net48, STA/message loop/await/atualização de controle sem janela visível.");
                    exit = 0;
                }
                catch (Exception ex) { Console.Error.WriteLine(ex); }
                finally { context.ExitThread(); }
            }));
            Application.Run(context);
            return exit;
        }
    }
}
