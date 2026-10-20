namespace Marketplace.Orders.Middleware;

// Única tabela código -> mensagem pt-BR. O cliente só vê o que está aqui.
public static class ErrorMessages
{
    private static readonly Dictionary<string, string> Messages = new()
    {
        ["order.not_found"] = "Pedido não encontrado",
        ["order.forbidden"] = "Você não tem acesso a este pedido",
        ["order.cannot_cancel"] = "Este pedido não pode mais ser cancelado",
        ["order.already_cancelled"] = "Este pedido já está cancelado",
        ["order.invalid_transition"] = "Transição de status inválida para o pedido",
        ["order.empty"] = "O pedido precisa de ao menos um item",
        ["order.product_not_found"] = "Produto não encontrado no catálogo",
        ["order.product_inactive"] = "Produto indisponível no momento",
        ["order.invalid_status"] = "Status de pedido inválido",
        ["auth.unauthorized"] = "Autenticação necessária",
        ["auth.forbidden"] = "Você não tem permissão para esta ação",
        ["validation.failed"] = "Dados inválidos",
        ["internal.error"] = "Erro interno. Tente novamente mais tarde",
    };

    public static string For(string code) => Messages.GetValueOrDefault(code, Messages["internal.error"]);
}
