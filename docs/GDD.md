# Game Design Document

**Bicudo - um toque, uma asa**

Autor: Fabrício Júnio Almeida Dias

---

## 1. Visão geral

| | |
|---|---|
| Gênero | Arcade de um botão, rolagem lateral |
| Plataforma | PC (Windows 64 bits) |
| Motor | Unity 2022.3 |
| Perspectiva | Lateral, 2D |
| Público | Livre |
| Duração de partida | de 5 segundos a alguns minutos |
| Jogadores | Um |

## 2. Conceito

Um pássaro atravessa uma fileira de canos sem fim. Ele cai o tempo todo, e o
único comando disponível faz ele subir um pouco. Não há vida extra, não há
fase, não há pausa: a partida dura até o primeiro encostão.

O jogo é sobre ritmo. A dificuldade não vem de decorar nada, vem de manter uma
cadência de toques que o jogador descobre sozinho nas primeiras tentativas.

## 3. A decisão central de design

**Um comando só, e ele troca a velocidade em vez de somar a ela.**

Essa é a regra que faz o gênero funcionar. Se o toque somasse força, dois
toques seguidos subiriam o dobro, e o jogo viraria uma disputa de quem aperta
mais rápido. Trocando a velocidade por um valor fixo, cada toque sobe sempre o
mesmo tanto, venha de onde vier: o jogador não controla a intensidade, controla
o momento.

Tudo o mais decorre disso. A gravidade é constante, a velocidade de queda tem
teto para dar tempo de reagir, e a abertura entre os canos é grande o bastante
para caber dois toques mal dados.

## 4. Fluxo da partida

```
Esperando  --toque-->  Voando  --encostou-->  Caiu  --toque-->  Esperando
```

Três estados na mesma cena, e não três telas. O jogo reinicia em um toque, e
carregar cena entre eles colocaria uma pausa onde não cabe pausa.

- **Esperando:** o pássaro balança parado, o céu rola devagar, o caminho está
  limpo de canos. Nada mata.
- **Voando:** gravidade ligada, canos chegando, placar contando.
- **Caiu:** o mundo para, o pássaro tomba, e a tela de fim mostra placar,
  medalha e recorde.

Na tela de fim há uma espera curta antes de aceitar o toque que reinicia. Quem
morre está batendo asa, e o mesmo toque que matou reiniciaria a partida antes
de a pessoa ver o próprio placar.

## 5. O pássaro

| | |
|---|---|
| Impulso | 6,2 unidades por segundo |
| Gravidade | 18 unidades por segundo ao quadrado |
| Queda máxima | 10 unidades por segundo |
| Raio de colisão | 0,28 unidade |

A inclinação acompanha a velocidade: bico para cima logo depois do toque, e
descendo até 70 graus para baixo no fim da queda. É enfeite, mas é o enfeite
que informa: dá para ver que está caindo antes de sentir.

O bater de asa fica mais rápido enquanto ele sobe, o que dá a impressão de
esforço no momento certo.

## 6. Os canos

| | |
|---|---|
| Abertura | 5 unidades (2,5 para cada lado do centro) |
| Espaçamento | 4,2 unidades entre um par e o seguinte |
| Velocidade | 3,2 unidades por segundo |
| Altura da passagem | sorteada entre -1 e 2,2 |

O pássaro mede uma unidade, então cabem cinco dele na passagem.

Uma fila fixa circula a partida inteira. O que sai pela esquerda volta para a
direita com uma abertura nova, em vez de ser destruído e recriado. Quantos
pares a fila tem não é número escolhido a dedo: depende da largura da tela, e
sai da conta descrita em 8.4.

O ponto é contado por posição, quando o cano fica para trás do pássaro. A
primeira versão usava uma portaria no meio da passagem e esperava o pássaro
entrar nela. Nunca disparou: o pássaro não tem colisor, porque detecta batida
por consulta de círculo, e gatilho do Unity precisa de colisor dos dois lados.
O placar ficava a partida inteira em zero. Comparar posição não depende de
física nenhuma e é o mesmo critério que o jogador enxerga.

Na tela inicial os pares esperam parados, à vista, ao longe. Escondê-los
custava caro dos dois lados: ou o primeiro aparecia do nada no meio da tela ao
começar a partida, ou esperava fora dela e a partida abria com vários segundos
de céu vazio.

## 7. Pontuação

| Pontos | Medalha |
|---|---|
| 5 | bronze |
| 15 | prata |
| 30 | ouro |

Os cortes são baixos de propósito. Num jogo em que a primeira dezena de
tentativas termina com dois ou três pontos, medalha que só aparece aos
cinquenta nunca aparece.

O recorde fica gravado na máquina e sobrevive entre partidas. Ele é gravado no
instante em que a partida termina, e não pela tela de fim: se dependesse da
interface, uma partida encerrada com a tela ainda não montada perderia a marca.

## 8. Como o jogo é montado

Nada foi arrastado na mão no editor. O menu **Bicudo** tem três passos, e
**MONTAR TUDO** roda os três na ordem:

1. **Preparar arte** copia os PNG dos pacotes e marca os recortes nos
   metadados de importação.
2. **Montar prefabs** cria pássaro e cano a partir dos recortes.
3. **Gerar cena** monta câmera, fundo, chão, canos, sistemas e interface.

A ordem importa: os prefabs dependem dos recortes e a cena depende dos
prefabs. Fora de ordem sai cena vazia, sem erro nenhum, que é o pior tipo de
falha para achar depois.

### 8.1 O recorte da arte

O recorte é feito pelo `ISpriteEditorDataProvider`. O caminho antigo,
`TextureImporter.spritesheet`, foi descontinuado: compila, não avisa nada e não
tem efeito. A folha inteira vira um sprite só e o pássaro aparece com os quatro
quadros lado a lado na tela.

Do cano saem só duas peças, a boca e um pedaço de corpo. O cano é montado
empilhando corpo até sair da tela, em vez de esticar um sprite: esticar deforma
o pixel e destoa do resto do desenho.

### 8.2 A colisão

O pássaro é movido pelo `transform`, sem corpo rígido. Nesse caso os eventos de
colisão do Unity não são confiáveis, porque entre dois quadros o objeto
atravessa o cano sem disparar nada.

A cada quadro o jogo pergunta se existe algo sólido na posição dele, com um
círculo só. É barato, e não deixa passar.

### 8.3 O chão que rola

Os tiles do chão rolam, o colisor não. Se o colisor rolasse junto, ele sairia de
cena e abriria um buraco no fim do mundo. O que rola é desenho; o que barra é
uma caixa larga e parada.

A reposição das faixas é feita em múltiplos exatos da largura, num laço `while`
e não num `if`: com um quadro longo, a peça pode passar do limite mais de uma
largura de uma vez.

### 8.4 Onde a peça troca de lugar

A altura visível é escolhida por nós, sete unidades para cada lado do centro. A
largura não: ela sai da proporção da tela, e proporção só existe quando o jogo
roda. Números de largura fixados na cena dão certo em um monitor e erram em
todos os outros.

Foi o que aconteceu. O chão sumia pela borda esquerda e voltava sozinho, e o
cano desaparecia poucos passos atrás do pássaro para reaparecer do nada à
frente dele. Nos dois casos a peça continuava sendo reaproveitada como sempre;
o que estava errado era o lugar onde a troca acontecia, dentro do campo de
visão.

`Enquadramento.MeiaLargura` mede quanto a câmera enxerga para cada lado. A
partir dela:

- **Canos:** entram e saem em `meia largura + 2`. A fila precisa de pares
  suficientes para que o par reposto caia atrás do último, e não dentro da
  tela: `quantidade × espaçamento` tem que passar da distância entre a entrada
  e a saída.
- **Faixas:** a peça volta para o fim da fila em `-(meia largura + largura da
  peça)`. A peça mais à esquerda nunca passa desse limite, então a fila só
  cobre de `limite + largura` em diante; com o limite valendo apenas
  `-largura`, que era o valor antigo, essa borda caía em zero e metade da tela
  ficava sem chão.
- **Quantas peças recortar** é decisão de montagem e não dá para refazer ao
  rodar, então a cena é gerada para a tela mais larga prevista, 32:9.

Se ainda assim a fila for curta demais para a tela, a faixa avisa no console em
vez de falhar calada, porque o sintoma é o cenário piscando na borda e nada
apontando para a causa.

A conta não é feita uma vez só. O mesmo executável abre num monitor ultrawide e
num notebook, e a janela pode virar tela cheia no meio da partida. Canos e
faixas comparam a meia largura a cada quadro e refazem o enquadramento quando
ela muda: as bordas se alargam, a fila de canos cresce se precisar, e as peças
de cenário se realinham. Realinhar não aparece na tela, porque as peças são
iguais entre si.

## 9. Áudio

Quatro efeitos, gerados por síntese quando o jogo acorda: bater de asa, ponto,
batida e queda. Onda quadrada com envelope de decaimento exponencial.

Nenhum arquivo de áudio de terceiros entra no projeto. Os dois pacotes usados
são de imagem, e trazer som de fora significaria uma terceira licença para
conferir e creditar, num jogo em que quatro bipes resolvem.

## 10. Interface

- **Placar** no alto, em fonte monoespaçada. Em fonte proporcional o "1" é bem
  mais estreito que o "8", e o número dança de lado a cada ponto.
- **Tela inicial** com título, subtítulo e os comandos aceitos.
- **Tela de fim** com placar, medalha, recorde e aviso de recorde novo.

Todo texto tem contorno escuro. O fundo do jogo muda de cor conforme a arte
escolhida, e sem contorno existe combinação em que o placar simplesmente some.

A interface acompanha a **altura** da tela, e não a largura: num monitor mais
largo que 16:9, o casamento por largura estica tudo até sair por cima e por
baixo.

## 11. Regras

1. O único comando é bater as asas.
2. O impulso troca a velocidade vertical, não soma a ela.
3. Encostar em cano ou chão encerra a partida.
4. Cada par de canos vale um ponto, uma vez só.
5. O recorde é gravado na máquina e não some ao reiniciar.
6. Na tela inicial nada mata e nenhum cano está perto do pássaro.
7. Não há vida extra, fase, pausa nem limite de tempo.

## 12. Créditos de arte

**Flappy Bird Assets**, de Megacrash (megacrash.itch.io). Paleta Endesga64.
Uso livre, comercial ou não. Pássaro, canos, chão e fundo.

**Cute Flappy**, de Gufiao Studios / Jovanny Rodriguez (gufiao.itch.io). Uso
pessoal e comercial permitido; a licença proíbe redistribuir os arquivos como
peças soltas, então eles ficam dentro do projeto e o recorte vive nos
metadados de importação.

O jogo original, Flappy Bird, é de Dong Nguyen. Este projeto é um exercício
sobre a mesma ideia, e não uma cópia do código dele.
