# Coleta automática com gMSA (sem segredo no host)

A coleta agendada do Direnix roda sob a identidade do **serviço Windows**, autenticando no AD via
**Kerberos/Negotiate** sobre **LDAPS**. Não armazenamos nenhuma senha de AD: a recomendação é uma
**gMSA (Group Managed Service Account)**, cuja senha é gerada e **rotacionada automaticamente** pelo
Active Directory. Assim não há segredo decifrável em disco, backup ou banco.

> A coleta é **somente leitura**. Use uma conta de **menor privilégio** (NÃO Domain Admin). A gMSA
> deve ter apenas direito de leitura no diretório.

## 1. Pré-requisitos no AD (uma vez)

```powershell
# 1) Chave raiz do KDS (uma vez por floresta). Em lab pode usar -EffectiveTime imediato:
Add-KdsRootKey -EffectiveTime ((Get-Date).AddHours(-10))   # produção: aguarde 10h de replicação

# 2) Criar a gMSA e permitir que o HOST que roda o serviço recupere a senha:
New-ADServiceAccount -Name svc-Direnix `
  -DNSHostName svc-Direnix.SEU-DOMINIO.local `
  -PrincipalsAllowedToRetrieveManagedPassword "NOME-DO-HOST$"

# 3) (Opcional) cobertura total de leitura: adicione a gMSA a um grupo de leitura/delegação.
#    Por padrão ela já lê a maior parte como usuário autenticado.
```

## 2. No host que roda o serviço

```powershell
Install-WindowsFeature RSAT-AD-PowerShell   # se necessário para os cmdlets
Install-ADServiceAccount svc-Direnix
Test-ADServiceAccount svc-Direnix          # deve retornar True
```

## 3. Configurar o serviço para rodar sob a gMSA

Há duas formas:

**a) Pelo portal (recomendado):** em **Operação → Configuração do serviço**, escolha
**Identidade = Conta gerenciada (gMSA)**, informe **só o nome da conta** (`SEU-DOMINIO\svc-Direnix$`,
sem senha), defina o **Tipo de inicialização** (Automático) e clique **Aplicar configuração**. Em
seguida **reinicie o serviço**. (A ação fica registrada na Auditoria.)

**b) Por linha de comando:**

```powershell
sc.exe config "Direnix.Service" obj= "SEU-DOMINIO\svc-Direnix$" password= "" start= auto
Restart-Service "Direnix.Service"
```

> Conceda à gMSA o direito **"Log on as a service"** (via GPO/secpol) antes de reiniciar.
> Trocar a conta do serviço para fora da gMSA invalida a premissa de "sem segredo" — reconfigure
> conscientemente.

## 4. Endurecimento do canal (recomendado no DC)

- **LDAP Signing** obrigatório e **LDAP Channel Binding** habilitados nos Domain Controllers
  (mitigam downgrade/relay de NTLM). A coleta agendada já força **LDAPS** + Kerberos sign+seal.
- Certificado LDAPS válido no DC; em laboratório com cert self-signed, confie no certificado do DC
  na máquina do serviço (ou use o pin de thumbprint quando disponível).

## 5. No portal

1. Primeiro acesso: criar o **administrador local** (login mínimo do portal) e entrar.
2. **Operação → Coleta automática**: habilitar, escolher frequência/horário, o controlador de
   domínio (LDAPS fixo) e o **Perfil de regras** que o agendamento usará. **Não há campo de senha** —
   a identidade é a do serviço (gMSA).
   - O **Perfil de regras** define o escopo da coleta agendada. Se deixar em **"Perfil ativo no
     momento"**, a coleta usa o perfil que estiver ativo quando rodar; escolha um perfil nomeado para
     fixar exatamente quais regras/tipos serão coletados todo dia.
3. **Testar conectividade**: valida que a gMSA consegue ler o RootDSE.
4. As execuções agendadas aparecem no histórico de avaliações e na **Auditoria** como operador
   `scheduler` (com o perfil usado nos detalhes).

## Auditoria

Toda ação do portal é registrada em **Operação → Auditoria de ações**: login/logout, criação do
admin, login falho, avaliação iniciada (manual e agendada), perfis salvos, exceções, salvar
agendamento, teste de conectividade e alteração da configuração do serviço (com data, operador e
resultado).

## Modelo de ameaça (resumo honesto)

Sem segredo em repouso, os vetores de roubo offline (disco/backup/cópia do DB) deixam de existir. O
risco residual é o **host comprometido em execução**: os tickets Kerberos da gMSA vivem na LSASS e um
atacante com SYSTEM pode abusar da identidade — porém ela é **somente leitura** (baixo impacto) e o AD
**rotaciona** a senha periodicamente, expirando material roubado. Por isso a escolha de menor
privilégio é a defesa mais importante.
