# Bicudo

Um toque, uma asa.

Jogo de um botão só em Unity 2022.3, na linha do Flappy Bird: o pássaro cai
sozinho, sobe quando você manda, e o jogo acaba no primeiro encostão.

**Autor:** Fabrício Júnio Almeida Dias

## Como abrir

1. Instale o **Unity 2022.3.62f3** pelo Unity Hub.
2. No Unity Hub, clique em **Add** e aponte para a pasta deste projeto.
3. Abra o projeto. Na barra de menus aparece o menu **Bicudo**.
4. Clique em **Bicudo → MONTAR TUDO**. Esse passo recorta a arte, monta os
   prefabs e gera a cena.
5. Abra `Assets/Cenas/Jogo.unity` e aperte **Play**.

O passo 4 é necessário porque o projeto se monta a partir do código: nada foi
arrastado na mão no editor.

## Como jogar

| Ação | Tecla |
|------|-------|
| Bater as asas | espaço, seta para cima, W ou clique |
| Começar | qualquer um dos acima |
| Tentar de novo | qualquer um dos acima, na tela de fim |

Cada cano atravessado vale um ponto. O recorde fica gravado na máquina. A
partir de 5 pontos sai medalha de bronze, aos 15 prata, aos 30 ouro.

## Como o jogo é montado

Uma cena só. A tela inicial, o voo e a tela de fim são três estados da mesma
cena, e não três cenas: o jogo reinicia em um toque, e carregar cena no meio
disso colocaria uma pausa onde não cabe pausa.

```
Bicudo/
  1. Preparar arte    copia os PNG e marca os recortes
  2. Montar prefabs   pássaro e cano, a partir dos recortes
  3. Gerar cena       câmera, fundo, chão, canos, sistemas e interface
  MONTAR TUDO         os três na ordem
```

### O pulo é uma troca, não uma força

O pássaro não usa `Rigidbody2D`. O impulso do Flappy Bird troca a velocidade
vertical por um valor fixo, e não soma a ela: é por isso que dois toques
seguidos sobem o mesmo tanto que um, e é isso que faz o jogo ser sobre ritmo.

Com corpo rígido, a altura do pulo passaria a depender da massa, do arrasto e
da gravidade do projeto, e ajustar o toque viraria tentativa e erro. Aqui a
altura é um número em unidades por segundo, legível no Inspector.

### A colisão é uma consulta, não um evento

Como o pássaro é movido pelo `transform`, os eventos de colisão do Unity não
são confiáveis: entre dois quadros ele atravessa o cano sem disparar nada. A
cada quadro o jogo pergunta se existe algo sólido no lugar dele, com um
círculo só, o que é barato e não deixa passar.

### Os canos são reaproveitados

Uma fila fixa circula a partida inteira: o que sai pela esquerda volta para a
direita com uma abertura nova. Criar e destruir um par a cada dois segundos
funcionaria, mas dá trabalho ao coletor de lixo justamente durante o jogo, que
é quando um engasgo custa caro.

### O cenário se enquadra ao rodar

A altura que a câmera enxerga é escolhida por nós; a largura sai da proporção
da tela, que só existe quando o jogo roda. Por isso o ponto em que o cano troca
de lugar e o ponto em que a peça de chão volta para o fim da fila são
calculados ao rodar, e não fixados na cena. Com eles fixos, a troca caía dentro
do campo de visão: o chão sumia pela borda e o cano reaparecia do nada à frente
do pássaro.

A medida é refeita sempre que a tela muda, e não só na abertura. O mesmo
executável abre num ultrawide e num notebook, e a janela pode virar tela cheia
no meio da partida.

## Som

Não há arquivo de áudio no projeto. Os quatro efeitos (bater de asa, ponto,
batida e queda) são gerados por síntese quando o jogo acorda: onda quadrada
com envelope de decaimento.

A escolha não é só técnica. Os dois pacotes de arte usados aqui são de imagem;
trazer som de terceiros significaria uma terceira licença para conferir e
creditar, num jogo em que quatro bipes resolvem.

## Testes

Rodam pelo Unity Test Framework, em EditMode e PlayMode.

```powershell
.\montar.ps1 -SoTestes
```

Ou pelo editor: **Window → General → Test Runner**.

Os grupos são três:

- **projeto** (EditMode) confere o resultado da montagem: se a arte foi
  recortada, se os prefabs existem, se nenhum desenho ficou sem sprite e se a
  cena entrou no build. O jogo se constrói por script, e um passo que falha em
  silêncio deixa a cena de pé mas vazia.
- **partida** e **voo** (PlayMode) cobrem as regras: o impulso que troca a
  velocidade em vez de somar, o ponto que não conta duas vezes no mesmo cano, o
  recorde que uma partida pior não derruba, e a fila de canos que não cria nem
  destrói nada durante o jogo.
- **cena real** (PlayMode) abre a cena que vai no executável e joga nela: um
  toque começa a partida, os canos andam, e o pássaro morre no chão. É o que
  pega referência não ligada e objeto fora da camada, que os testes de
  cenário montado à mão não veem.

## Créditos de arte

**Pássaro, canos, chão e fundo:** [Flappy Bird Assets](https://megacrash.itch.io/flappy-bird-assets),
de Megacrash. Paleta Endesga64. Uso livre, comercial ou não.

**Pacote alternativo:** Cute Flappy, de
[Gufiao Studios](https://gufiao.itch.io) (Jovanny Rodriguez). Uso pessoal e
comercial permitido; a licença proíbe redistribuir os arquivos como peças
soltas, então eles ficam dentro do projeto e o recorte vive nos metadados de
importação, sem gerar cópia alterada da arte.

O jogo original, Flappy Bird, é de Dong Nguyen. Este projeto é um exercício
sobre a mesma ideia, não uma cópia do código dele.

## Estrutura

```
Assets/
  Arte/         os PNG dos pacotes, sem alteração
  Editor/       geradores: arte, prefabs, cena e build
  Prefabs/      pássaro e cano, montados por script
  Scripts/
    Partida        estado do jogo, placar e recorde
    Passaro        gravidade, impulso, inclinação e colisão
    Cano           o par e a trava do ponto
    Gerador        a fila de canos que circula
    Faixa          chão e céu rolando sem emenda
    Enquadramento  quanto a câmera enxerga para cada lado
    Sons           efeitos gerados por síntese
    UI/            a tela, nos três estados
  Tests/        EditMode e PlayMode
docs/           GDD
```
