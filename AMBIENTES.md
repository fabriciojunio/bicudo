# Homologação e Produção

| Branch | Uso |
|---|---|
| `homologação` | Integração das alterações e validação antes da publicação. |
| `produção` | Branch padrão; recebe alterações verificadas por pull request. |

O fluxo é: branch de trabalho → `homologação` → `produção`. Preserve os nomes com acentuação nos comandos Git e nas configurações de integração.

Antes de promover uma alteração, execute os testes e verificações aplicáveis, revise as diferenças e confira migrações, configuração e procedimento de reversão. Uma falha de teste deve ser corrigida ou investigada antes da promoção.

Este repositório ainda não tem um workflow de testes versionado. A criação das branches não substitui a validação local específica do projeto.

As branches organizam o código. Um ambiente publicado de homologação exige configuração própria de hospedagem, banco e credenciais; não reutilize dados pessoais nem segredos de produção nos testes. A existência da branch não comprova que esses recursos já estejam provisionados.

Na hospedagem conectada ao Git, configure `produção` como origem das versões públicas e `homologação` como origem das versões de validação, quando houver esse recurso. Endereços, variáveis e permissões devem ser conferidos por ambiente.
