# systemPatterns.md

## Padrões Gerais do Projeto Smart Alarm

### Arquitetura
- Siga Clean Architecture e princípios SOLID (backend) e Atomic Design (frontend).
- Separe claramente camadas de domínio, aplicação, infraestrutura e apresentação (backend) e componentes, páginas, hooks e contextos (frontend).
- Use injeção de dependência para facilitar testes e manutenção (backend) e composição de componentes (frontend).
- Não introduza tipos ou valores no escopo global.

### Organização de Código
- Agrupe arquivos por domínio de negócio e responsabilidade (backend) e por feature/component (frontend).
- Mantenha testes próximos ao código implementado.
- No frontend, organize componentes em pastas por atomicidade (atoms, molecules, organisms, pages).
- Documente decisões arquiteturais em `docs/architecture/`.

### Nomenclatura
- camelCase para variáveis, funções e métodos.
- PascalCase para classes, tipos, componentes e arquivos React.
- UPPER_SNAKE_CASE para constantes globais.
- Nomes descritivos e claros, sem abreviações.

### JavaScript/TypeScript (Frontend)
- Use aspas duplas para strings visíveis ao usuário e aspas simples para strings internas.
- Sempre use ponto e vírgula ao final das declarações.
- Prefira const para variáveis imutáveis e let para mutáveis. Evite var.
- Use arrow functions `=>` e só coloque parênteses nos parâmetros quando necessário.
- Sempre use chaves em condicionais e loops, com a chave de abertura na mesma linha.
- Use JSDoc para documentar funções, classes e interfaces públicas (backend) e TypeScript types/interfaces para props e estados (frontend).
- Não exporte tipos ou funções desnecessariamente.
- No frontend, use React.FC para componentes funcionais e prefira hooks para lógica reutilizável.

### Testes
- Escreva testes unitários para toda lógica de negócio (Vitest ou Jest).
- Inclua casos de sucesso, falha e cenários extremos.
- Use mocks para dependências externas.
- Nomeie testes de forma descritiva (ex: "deve retornar erro se...", "should return error if...").
- Siga o padrão AAA (Arrange, Act, Assert).
- No frontend, use Testing Library para componentes React, cubra interações, acessibilidade e estados visuais.

### Tratamento de Erros
- Use try/catch para capturar exceções.
- Prefira lançar erros específicos.
- Sempre registre erros com contexto relevante.
- Valide todas as entradas do usuário e dados externos.
- No frontend, trate erros de API e exiba mensagens amigáveis ao usuário.

### Segurança
- Nunca exponha credenciais ou segredos no código.
- Use variáveis de ambiente para dados sensíveis.
- Valide e sanitize entradas do usuário.
- Não registre informações sensíveis em logs.
- No frontend, nunca exponha tokens ou segredos em código ou no bundle.
- Implemente autenticação e autorização no consumo de APIs.
- Siga práticas de acessibilidade (WCAG) e privacidade (LGPD) na interface.

### Backend (APIs e Serviços)
- Siga Clean Architecture e princípios SOLID para toda lógica de backend.
- Separe controladores, serviços, repositórios e entidades.
- Use DTOs para entrada e saída de dados.
- Implemente autenticação e autorização conforme necessidade do domínio.
- Sempre valide e sanitize dados recebidos em endpoints.
- Use logs estruturados para rastrear requisições e erros, sem expor dados sensíveis.
- Implemente testes unitários e de integração para endpoints e serviços críticos.
- Documente endpoints e contratos de API (ex: Swagger/OpenAPI).
- Prefira middlewares para tratamento de erros e autenticação.
- Nunca exponha segredos ou variáveis sensíveis em código ou logs.

### Frontend (React/PWA)
- Siga Atomic Design para organização de componentes.
- Use React, TypeScript e hooks para lógica de UI.
- Separe componentes por atomicidade (atoms, molecules, organisms, pages).
- Use context API para estado global e hooks customizados para lógica compartilhada.
- Implemente acessibilidade (WCAG), responsividade e internacionalização.
- Utilize Service Workers para PWA e notificações.
- Teste componentes com Testing Library e simule interações reais.
- Documente props e contratos de componentes com TypeScript.

### Integrações e APIs
- Use ferramentas e melhores práticas para OCI, OpenAI, GitHub e outras integrações.
- Sempre consulte as instruções específicas para cada serviço (ex: OCI Functions, SWA, etc).
- No frontend, consuma APIs via HttpClient/Fetch, trate erros e estados de loading.

### Fluxo de Desenvolvimento
- Instale dependências com `npm install`.
- Compile com `npm run compile` (ou `npm run build` no frontend).
- Execute testes com `npm run test:unit` e `npm run test:integration`.
- Use scripts de simulação e integração para validar cenários completos.
- No frontend, utilize linters (ESLint), formatadores (Prettier) e verifique acessibilidade (axe, Lighthouse).

### Revisão e Pull Requests
- Siga o formato de commits convencionais.
- Descreva claramente o que mudou e por quê.
- Inclua contexto, alterações, testes realizados e pendências na descrição do PR.
- No frontend, revise acessibilidade, responsividade e impacto visual das mudanças.

### Exemplos de Boas Práticas

#### Função Assíncrona (Backend)
```csharp
public async Task<User> GetUserByIdAsync(Guid id)
{
    if (id == Guid.Empty)
        throw new ArgumentException("ID é obrigatório");
    var user = await _userRepository.GetByIdAsync(id);
    if (user == null)
        throw new NotFoundException("Usuário não encontrado");
    return user;
}
```

#### Teste Unitário (Backend)
```csharp
[Fact]
public async Task Should_ThrowArgumentException_When_IdIsEmpty()
{
    var service = new UserService(...);
    await Assert.ThrowsAsync<ArgumentException>(() => service.GetUserByIdAsync(Guid.Empty));
}
```

#### Componente React (Frontend)
```tsx
import React from "react";

type ButtonProps = {
    label: string;
    onClick: () => void;
};

export const Button: React.FC<ButtonProps> = ({ label, onClick }) => (
    <button onClick={onClick} aria-label={label}>
        {label}
    </button>
);
```

#### Teste de Componente (Frontend)
```typescript
import { render, screen, fireEvent } from "@testing-library/react";
import { Button } from "./Button";

test("deve chamar onClick ao clicar", () => {
    const onClick = vi.fn();
    render(<Button label="Salvar" onClick={onClick} />);
    fireEvent.click(screen.getByRole("button"));
    expect(onClick).toHaveBeenCalled();
});
```
