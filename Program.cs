using System.Diagnostics;

namespace ParalelismoDemo
{
    class Program
    {
        private static readonly HttpClient _httpClient = new();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Demonstração de paralelismo e programação assíncrona em .NET");
            Console.WriteLine("===========================================================\n");

            await ExecutarDemonstracoes();

            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        static async Task ExecutarDemonstracoes()
        {
            // Exemplo 1: Task.WhenAny - Primeira API a responder
            await DemonstrarTaskWhenAny();

            // Exemplo 2: Task.WhenAll - Processamento paralelo de múltiplas tarefas
            await DemonstrarTaskWhenAll();

            // Exemplo 3: IAsyncEnumerable - Streaming assíncrono
            await DemonstrarIAsyncEnumerable();

            // Exemplo 4: Parallel.ForEach - Paralelismo de CPU
            DemonstrarParallelForEach();

            // exemplo 5: Cancelamento de tarefas
            await DemonstrarCancelamento();
        }

        static async Task DemonstrarTaskWhenAny()
        {
            Console.WriteLine("\n exemplo task.whenAny - primeira api a responder");
            Console.WriteLine("----------------------------------------------------");

            var sw = Stopwatch.StartNew();

            var apis = new[]
            {
                "https://jsonplaceholder.typicode.com/posts/1",
                "https://jsonplaceholder.typicode.com/posts/2",
                "https://jsonplaceholder.typicode.com/posts/3"
            };

            List<Task<string>> tarefas = apis.Select(url => 
                BuscarDadosComDelayAleatorio(url)).ToList();

            Task<string> primeiraResposta = await Task.WhenAny(tarefas);
            string resultado = await primeiraResposta;

            sw.Stop();

            Console.WriteLine($"Primeira resposta recebida em {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Resposta: {resultado.Substring(0, Math.Min(50, resultado.Length))}...");
            Console.WriteLine("As outras chamadas API continuam em background.");

            await Task.WhenAll(tarefas);
            Console.WriteLine($"Todas as APIs completaram em {sw.ElapsedMilliseconds}ms");
        }

        static async Task<string> BuscarDadosComDelayAleatorio(string url)
        {
            // Simular latência de rede variável
            var random = new Random();
            int delay = random.Next(500, 3000);
            await Task.Delay(delay);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        static async Task DemonstrarTaskWhenAll()
        {
            Console.WriteLine("\n2. Exemplo task.WhenAll - processamento paralelo");
            Console.WriteLine("----------------------------------------------------");

            var sw = Stopwatch.StartNew();

            var ids = Enumerable.Range(1, 5).ToList();

            //Abordagem sequencial
            sw.Restart();
            foreach (var id in ids)
            {
                await ProcessarItemSequencial(id);
            }
            sw.Stop();
            Console.WriteLine($"Processamento sequencial: {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            var tarefas = ids.Select(id => ProcessarItemParalelo(id));
            await Task.WhenAll(tarefas);
            sw.Stop();
            Console.WriteLine($"Processamento paralelo: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Ganho de performance: {(double)sw.ElapsedMilliseconds / ids.Count:F1}ms/item");
        }

        static async Task ProcessarItemSequencial(int id)
        {
            //Simular processamento que leva tempo
            await Task.Delay(300);
        }

        static async Task ProcessarItemParalelo(int id)
        {
            //mesmo processamento mas em paralelo
            await Task.Delay(300);
        }

        static async Task DemonstrarIAsyncEnumerable()
        {
            Console.WriteLine("\n3. exemplo IAsyncEnumerable - streaming assincrono");
            Console.WriteLine("----------------------------------------------------");

            Console.WriteLine("Processando itens conforme fica disponiveis:");

            var sw = Stopwatch.StartNew();

            int count = 0;

            await foreach (var item in GerarItensDinamicamente())
            {
                count++;
                Console.WriteLine($" Item {count} processado em T+{sw.ElapsedMilliseconds}ms: {item}");
            }

            sw.Stop();
            Console.WriteLine($"Total: {count} itens em {sw.ElapsedMilliseconds}");
        }

        static async IAsyncEnumerable<string> GerarItensDinamicamente()
        {
            for (int i = 1; i <= 5; i++)
            {
                // Simular tempo variável para cada item ficar pronto
                await Task.Delay(500 * i);
                yield return $"Dados do item {i}";
            }
        }

        static void DemonstrarParallelForEach()
        {
            Console.WriteLine("\n4. Exemplo parallel foreach - paralelismo de CPU");
            Console.WriteLine("------------------------------------------------");

            var numeros = Enumerable.Range(1, 20).ToList();

            var sw = Stopwatch.StartNew();
            var resultadosSeq = new List<int>();
            foreach (var numero in numeros)
            {
                var resultado = ProcessarNumeroCPU(numero);
                resultadosSeq.Add(resultado);
            }
            sw.Stop();
            Console.WriteLine($"Processamento sequencial: {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            var resultadosPar = new List<int>();
            var lockObj = new object();

            Parallel.ForEach(numeros, numero =>
            {
                var resultado = ProcessarNumeroCPU(numero);

                lock (lockObj)
                {
                    resultadosPar.Add(resultado);
                }
            });

            sw.Stop();
            Console.WriteLine($"Processamento paralelo: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Speedup: {(double)resultadosSeq.Count / resultadosPar.Count:F2}x mais itens processados");
        }

        static int ProcessarNumeroCPU(int numero)
        {
            // Simular trabalho intensivo de CPU
            Thread.Sleep(100);
            return numero * numero;
        }

        static async Task DemonstrarCancelamento()
        {
            Console.WriteLine("\n5. exemplo de cancelamento de tarefas");
            Console.WriteLine("-------------------------------------");

            // Criar um token de cancelamento com timeout de 2 segundos
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            try
            {
                Console.WriteLine("Iniciando operação de longa duração com timeout de 2 segundos...");
                await OperacaoLongaDuracao(cts.Token);
                Console.WriteLine("Operação completada com sucesso!");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operação cancelada pelo timeout!");
            }

            //Exemplo de cancelamento manual
            using var ctsManual = new CancellationTokenSource();

            //Iniciar uma tarefa para cancelar após 1 segundo
            var cancellationTask = Task.Run(async () =>
            {
                await Task.Delay(1000);
                Console.WriteLine("Cancelando manualmente a operação...");
                ctsManual.Cancel();
            });

            try
            {
                Console.WriteLine("Iniciando operação que será cancelada manualmente...");
                await OperacaoLongaDuracao(ctsManual.Token);
                Console.WriteLine("Esta linha não deve ser executada!");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operação cancelada manualmente com sucesso!");
            }
        }

        static async Task OperacaoLongaDuracao(CancellationToken token)
        {
            for (int i = 0; i < 10; i++)
            {
                //Verificar cancelamento antes de cada iteração
                token.ThrowIfCancellationRequested();

                await Task.Delay(500, token);
                Console.WriteLine($"  Operação em andamento: {(i + 1) * 10}% concluido");
            }
        }
    }
}