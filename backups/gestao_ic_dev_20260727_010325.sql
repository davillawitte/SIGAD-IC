--
-- PostgreSQL database dump
--

-- Dumped from database version 16.1
-- Dumped by pg_dump version 16.1

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: Cargo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Cargo" (
    "Id" uuid NOT NULL,
    "Nome" character varying(150) NOT NULL,
    "Codigo" character varying(80) NOT NULL,
    "Ativo" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100)
);


--
-- Name: Escala; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Escala" (
    "Id" uuid NOT NULL,
    "SetorId" uuid NOT NULL,
    "Status" character varying(40) NOT NULL,
    "Observacao" character varying(1000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100),
    "Ano" integer DEFAULT 0 NOT NULL,
    "Mes" integer DEFAULT 0 NOT NULL
);


--
-- Name: EscalaJornada; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."EscalaJornada" (
    "Id" uuid NOT NULL,
    "EscalaServidorId" uuid NOT NULL,
    "TipoJornada" character varying(40) NOT NULL,
    "DataInicio" date NOT NULL,
    "DataFim" date NOT NULL,
    "HoraInicio" time without time zone,
    "HoraFim" time without time zone,
    "Horas" numeric(5,2),
    "TipoOcorrenciaCodigo" character varying(10) NOT NULL,
    "RecorrenciaTipo" character varying(40) NOT NULL,
    "DiasSemana" character varying(40),
    "IntervaloDias" integer,
    "DiasTrabalho" integer,
    "DiasFolga" integer,
    "TipoOcorrenciaFolgaCodigo" character varying(10),
    "Observacao" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100)
);


--
-- Name: EscalaOcorrencia; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."EscalaOcorrencia" (
    "Id" uuid NOT NULL,
    "EscalaServidorId" uuid NOT NULL,
    "Data" date NOT NULL,
    "TipoOcorrenciaCodigo" character varying(10) NOT NULL,
    "HoraInicio" time without time zone,
    "HoraFim" time without time zone,
    "Horas" numeric(5,2),
    "Origem" character varying(20) NOT NULL,
    "EscalaJornadaId" uuid,
    "Observacao" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100)
);


--
-- Name: EscalaServidor; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."EscalaServidor" (
    "Id" uuid NOT NULL,
    "EscalaId" uuid NOT NULL,
    "ServidorId" uuid NOT NULL,
    "CargoId" uuid NOT NULL,
    "Ordem" integer NOT NULL,
    "ServidorNome" character varying(200) NOT NULL,
    "Matricula" character varying(20) NOT NULL,
    "CargoNome" character varying(150) NOT NULL,
    "CargoCodigo" character varying(80) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100)
);


--
-- Name: Nucleo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Nucleo" (
    "Id" uuid NOT NULL,
    "Nome" character varying(150) NOT NULL,
    "ChefeServidorId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100),
    "Sigla" character varying(40) NOT NULL
);


--
-- Name: Perfil; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Perfil" (
    "Id" uuid NOT NULL,
    "Nome" character varying(120) NOT NULL,
    "Codigo" character varying(80) NOT NULL,
    "Descricao" character varying(500),
    "Sistema" boolean NOT NULL,
    "Ativo" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100)
);


--
-- Name: PerfilPermissao; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PerfilPermissao" (
    "PerfilId" uuid NOT NULL,
    "PermissaoId" uuid NOT NULL
);


--
-- Name: Permissao; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Permissao" (
    "Id" uuid NOT NULL,
    "Codigo" character varying(100) NOT NULL,
    "Nome" character varying(150) NOT NULL,
    "Descricao" character varying(500),
    "Modulo" character varying(80) NOT NULL,
    "Sistema" boolean NOT NULL,
    "Ativo" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100)
);


--
-- Name: Servidor; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Servidor" (
    "Id" uuid NOT NULL,
    "Nome" character varying(200) NOT NULL,
    "Matricula" character varying(50) NOT NULL,
    "Cpf" character varying(11) NOT NULL,
    "Email" character varying(200) NOT NULL,
    "Telefone" character varying(30),
    "SetorId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100),
    "CargoId" uuid NOT NULL,
    "Status" character varying(20) NOT NULL,
    "DataNascimento" date NOT NULL
);


--
-- Name: Setor; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Setor" (
    "Id" uuid NOT NULL,
    "Nome" character varying(150) NOT NULL,
    "Sigla" character varying(40) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100),
    "NucleoId" uuid,
    "Resumo" character varying(500)
);


--
-- Name: SetorChefia; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SetorChefia" (
    "SetorId" uuid NOT NULL,
    "TipoChefia" character varying(40) NOT NULL,
    "ServidorId" uuid NOT NULL
);


--
-- Name: TipoOcorrencia; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TipoOcorrencia" (
    "Codigo" character varying(10) NOT NULL,
    "Nome" character varying(120) NOT NULL,
    "HorasPadrao" numeric(5,2),
    "Categoria" character varying(40) NOT NULL,
    "Ativo" boolean NOT NULL
);


--
-- Name: Usuario; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Usuario" (
    "Id" uuid NOT NULL,
    "ServidorId" uuid NOT NULL,
    "Login" character varying(100) NOT NULL,
    "SenhaHash" character varying(500) NOT NULL,
    "UltimoLogin" timestamp with time zone,
    "Bloqueado" boolean NOT NULL,
    "Ativo" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100)
);


--
-- Name: UsuarioPerfil; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UsuarioPerfil" (
    "UsuarioId" uuid NOT NULL,
    "PerfilId" uuid NOT NULL
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Data for Name: Cargo; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Cargo" ("Id", "Nome", "Codigo", "Ativo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
174cf381-fb97-4301-aa54-227cbc179b23	Perito Criminal	PERITO_CRIMINAL	t	2026-07-21 22:04:52.265194-03	\N	migration	\N
9b80a4b6-4a6c-43c1-a08c-a30e1814644b	Agente Técnico Forense	AGENTE_TECNICO_FORENSE	t	2026-07-21 22:04:52.265194-03	\N	migration	\N
df9ad520-3051-4d7e-9f54-d43f7c13fc3e	Agente de Necrópsia	AGENTE_NECROPSIA	t	2026-07-21 22:04:52.265194-03	\N	migration	\N
76470c0b-7c46-456f-8c10-053e75a92358	Assistente Técnico Forense	ASSISTENTE_TECNICO_FORENSE	t	2026-07-21 22:04:52.265194-03	\N	migration	\N
61b2394f-bdb8-4344-888c-e78e01a7f5e6	Estagiário	ESTAGIARIO	t	2026-07-21 22:04:52.265194-03	\N	migration	\N
86ebd46f-50f1-483b-b8ee-e84ae7d8205a	Terceirizado	TERCEIRIZADO	t	2026-07-21 22:04:52.265194-03	\N	migration	\N
661d799f-4307-463e-9215-dd84698c5d98	Servidor Externo	SERVIDOR_EXTERNO	t	2026-07-21 22:04:52.265194-03	\N	migration	\N
\.


--
-- Data for Name: Escala; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Escala" ("Id", "SetorId", "Status", "Observacao", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "Ano", "Mes") FROM stdin;
\.


--
-- Data for Name: EscalaJornada; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."EscalaJornada" ("Id", "EscalaServidorId", "TipoJornada", "DataInicio", "DataFim", "HoraInicio", "HoraFim", "Horas", "TipoOcorrenciaCodigo", "RecorrenciaTipo", "DiasSemana", "IntervaloDias", "DiasTrabalho", "DiasFolga", "TipoOcorrenciaFolgaCodigo", "Observacao", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
\.


--
-- Data for Name: EscalaOcorrencia; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."EscalaOcorrencia" ("Id", "EscalaServidorId", "Data", "TipoOcorrenciaCodigo", "HoraInicio", "HoraFim", "Horas", "Origem", "EscalaJornadaId", "Observacao", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
\.


--
-- Data for Name: EscalaServidor; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."EscalaServidor" ("Id", "EscalaId", "ServidorId", "CargoId", "Ordem", "ServidorNome", "Matricula", "CargoNome", "CargoCodigo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
\.


--
-- Data for Name: Nucleo; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Nucleo" ("Id", "Nome", "ChefeServidorId", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "Sigla") FROM stdin;
\.


--
-- Data for Name: Perfil; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Perfil" ("Id", "Nome", "Codigo", "Descricao", "Sistema", "Ativo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	Super Administrador	SUPERADMINISTRADOR	Acesso total à plataforma	t	t	2026-07-19 23:18:48.240848-03	\N	seed	\N
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	Chefe de Setor	CHEFE_SETOR	Gestão operacional do setor	t	t	2026-07-19 23:18:48.259364-03	\N	seed	\N
cccccccc-cccc-cccc-cccc-cccccccccccc	Servidor	SERVIDOR	Acesso básico do servidor	t	t	2026-07-19 23:18:48.261815-03	\N	seed	\N
\.


--
-- Data for Name: PerfilPermissao; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PerfilPermissao" ("PerfilId", "PermissaoId") FROM stdin;
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	19b2a7c2-088f-6f42-83d6-dc2df12f7785
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	2b3c2c42-0647-7240-b263-15139f06f19b
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	350039ae-f8ad-7243-b5e6-cf618d30214e
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	397115eb-4e17-1b41-92b7-6ca39cf58ada
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	42019a35-967e-8b45-9a27-4280f1e4e1ff
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	47488808-3939-9345-b668-8ac46b0061eb
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	48f58cf8-ae76-624c-83ab-c1229e778c73
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	4c94c3ae-3bcd-4447-8435-9a1a5f7581dd
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	5685c66f-666b-0344-8671-34035e2aa378
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	7245e7f0-61f6-0c45-be8d-cca2073b6070
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	7420a328-9918-d240-a575-d2a4259f2eb5
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	7699093b-5ce9-fd4f-92ad-ebcd8eaa390d
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	89195584-2a56-0740-aeea-42c73c2d79d8
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	9c6c5628-69e7-b744-bf2c-7cd0439bc9f4
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	a59dd1f5-f7ac-5c40-8b3c-64e7c942a45c
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	aad0bb28-1c4b-b542-98c0-7eeb5b9d454c
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	b101efd3-9c98-8c4d-aaa0-85585c3b3cd7
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	bd74f1d4-ae3e-9e45-a6f4-92a1d7a52393
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	f583299f-145e-eb46-bc56-ed35ee4fd798
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	19b2a7c2-088f-6f42-83d6-dc2df12f7785
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	47488808-3939-9345-b668-8ac46b0061eb
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	4c94c3ae-3bcd-4447-8435-9a1a5f7581dd
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	5685c66f-666b-0344-8671-34035e2aa378
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	7245e7f0-61f6-0c45-be8d-cca2073b6070
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	89195584-2a56-0740-aeea-42c73c2d79d8
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	b101efd3-9c98-8c4d-aaa0-85585c3b3cd7
cccccccc-cccc-cccc-cccc-cccccccccccc	4c94c3ae-3bcd-4447-8435-9a1a5f7581dd
cccccccc-cccc-cccc-cccc-cccccccccccc	b101efd3-9c98-8c4d-aaa0-85585c3b3cd7
\.


--
-- Data for Name: Permissao; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Permissao" ("Id", "Codigo", "Nome", "Descricao", "Modulo", "Sistema", "Ativo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
19b2a7c2-088f-6f42-83d6-dc2df12f7785	perfis.listar	Listar perfis	Visualizar perfis de acesso	perfis	t	t	2026-07-19 23:18:48.208412-03	\N	seed	\N
2b3c2c42-0647-7240-b263-15139f06f19b	permissoes.excluir	Excluir permissões	Desativar permissões	permissoes	t	t	2026-07-19 23:18:48.208499-03	\N	seed	\N
350039ae-f8ad-7243-b5e6-cf618d30214e	perfis.editar	Editar perfis	Alterar dados de perfis	perfis	t	t	2026-07-19 23:18:48.208434-03	\N	seed	\N
397115eb-4e17-1b41-92b7-6ca39cf58ada	perfis.excluir	Excluir perfis	Desativar ou remover perfis	perfis	t	t	2026-07-19 23:18:48.208443-03	\N	seed	\N
42019a35-967e-8b45-9a27-4280f1e4e1ff	setores.criar	Criar setores	Cadastrar setores	setores	t	t	2026-07-19 23:18:48.208521-03	\N	seed	\N
47488808-3939-9345-b668-8ac46b0061eb	servidores.editar	Editar servidores	Alterar servidores	servidores	t	t	2026-07-19 23:18:48.208562-03	\N	seed	\N
48f58cf8-ae76-624c-83ab-c1229e778c73	permissoes.editar	Editar permissões	Alterar permissões	permissoes	t	t	2026-07-19 23:18:48.20849-03	\N	seed	\N
4c94c3ae-3bcd-4447-8435-9a1a5f7581dd	servidores.listar	Listar servidores	Visualizar servidores	servidores	t	t	2026-07-19 23:18:48.20854-03	\N	seed	\N
5685c66f-666b-0344-8671-34035e2aa378	usuarios.listar	Listar usuários	Visualizar usuários do sistema	usuarios	t	t	2026-07-19 23:18:48.149782-03	\N	seed	\N
7245e7f0-61f6-0c45-be8d-cca2073b6070	servidores.criar	Criar servidores	Cadastrar servidores	servidores	t	t	2026-07-19 23:18:48.208549-03	\N	seed	\N
7420a328-9918-d240-a575-d2a4259f2eb5	perfis.criar	Criar perfis	Cadastrar novos perfis	perfis	t	t	2026-07-19 23:18:48.208425-03	\N	seed	\N
7699093b-5ce9-fd4f-92ad-ebcd8eaa390d	permissoes.criar	Criar permissões	Cadastrar novas permissões	permissoes	t	t	2026-07-19 23:18:48.208481-03	\N	seed	\N
89195584-2a56-0740-aeea-42c73c2d79d8	permissoes.listar	Listar permissões	Visualizar catálogo de permissões	permissoes	t	t	2026-07-19 23:18:48.208471-03	\N	seed	\N
9c6c5628-69e7-b744-bf2c-7cd0439bc9f4	usuarios.bloquear	Bloquear usuários	Bloquear ou desbloquear acesso	usuarios	t	t	2026-07-19 23:18:48.208377-03	\N	seed	\N
a59dd1f5-f7ac-5c40-8b3c-64e7c942a45c	setores.editar	Editar setores	Alterar setores	setores	t	t	2026-07-19 23:18:48.208531-03	\N	seed	\N
aad0bb28-1c4b-b542-98c0-7eeb5b9d454c	perfis.gerenciar_permissoes	Gerenciar permissões do perfil	Associar ou remover permissões de um perfil	perfis	t	t	2026-07-19 23:18:48.208457-03	\N	seed	\N
b101efd3-9c98-8c4d-aaa0-85585c3b3cd7	setores.listar	Listar setores	Visualizar setores	setores	t	t	2026-07-19 23:18:48.208508-03	\N	seed	\N
bd74f1d4-ae3e-9e45-a6f4-92a1d7a52393	usuarios.editar	Editar usuários	Alterar dados e perfis de usuários	usuarios	t	t	2026-07-19 23:18:48.208356-03	\N	seed	\N
f583299f-145e-eb46-bc56-ed35ee4fd798	usuarios.criar	Criar usuários	Cadastrar novos usuários	usuarios	t	t	2026-07-19 23:18:48.207772-03	\N	seed	\N
\.


--
-- Data for Name: Servidor; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Servidor" ("Id", "Nome", "Matricula", "Cpf", "Email", "Telefone", "SetorId", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "CargoId", "Status", "DataNascimento") FROM stdin;
22222222-2222-2222-2222-222222222222	Vitor Lopes	00.000-1	00000000000	vitorlopes@pci.rn.gov.br	\N	11111111-1111-1111-1111-111111111111	2026-07-19 23:18:48.450314-03	\N	seed	\N	174cf381-fb97-4301-aa54-227cbc179b23	Ativo	1990-01-01
\.


--
-- Data for Name: Setor; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Setor" ("Id", "Nome", "Sigla", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "NucleoId", "Resumo") FROM stdin;
11111111-1111-1111-1111-111111111111	Direção do Instituto de Criminalística	Direção IC	2026-07-19 23:18:48.410335-03	2026-07-19 23:18:48.50026-03	seed	seed	\N	Direção geral do Instituto de Criminalística
\.


--
-- Data for Name: SetorChefia; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SetorChefia" ("SetorId", "TipoChefia", "ServidorId") FROM stdin;
11111111-1111-1111-1111-111111111111	Diretor	22222222-2222-2222-2222-222222222222
\.


--
-- Data for Name: TipoOcorrencia; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TipoOcorrencia" ("Codigo", "Nome", "HorasPadrao", "Categoria", "Ativo") FROM stdin;
\.


--
-- Data for Name: Usuario; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Usuario" ("Id", "ServidorId", "Login", "SenhaHash", "UltimoLogin", "Bloqueado", "Ativo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
33333333-3333-3333-3333-333333333333	22222222-2222-2222-2222-222222222222	vitorlopes	AQAAAAIAAYagAAAAEM2vDrjjcy4oP3xpOSqIgsqwkQP8rb0a8AvpwgYjQw7A+gR3s85cprwtS4C644Ti/A==	2026-07-19 23:19:16.500787-03	f	t	2026-07-19 23:18:48.554646-03	2026-07-19 23:19:16.500787-03	seed	vitorlopes
\.


--
-- Data for Name: UsuarioPerfil; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."UsuarioPerfil" ("UsuarioId", "PerfilId") FROM stdin;
33333333-3333-3333-3333-333333333333	aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20260707021157_InitialCreate	10.0.0
20260720021826_AddAuthRbac	10.0.0
20260721210920_AddCargoTable	10.0.0
20260721222628_AddNucleoAndEvolveSetor	10.0.0
20260721231633_AddSetorChefia	10.0.0
20260722001100_AddNucleoSigla	10.0.0
20260722003310_AddServidorDataNascimentoAndStatus	10.0.0
20260722010426_RemoveNucleoSetorAtivo	10.0.0
20260722013107_AddEscalaModule	10.0.0
20260722015341_EscalaMensalAnoMes	10.0.0
\.


--
-- Name: Cargo PK_Cargo; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Cargo"
    ADD CONSTRAINT "PK_Cargo" PRIMARY KEY ("Id");


--
-- Name: Escala PK_Escala; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Escala"
    ADD CONSTRAINT "PK_Escala" PRIMARY KEY ("Id");


--
-- Name: EscalaJornada PK_EscalaJornada; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaJornada"
    ADD CONSTRAINT "PK_EscalaJornada" PRIMARY KEY ("Id");


--
-- Name: EscalaOcorrencia PK_EscalaOcorrencia; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaOcorrencia"
    ADD CONSTRAINT "PK_EscalaOcorrencia" PRIMARY KEY ("Id");


--
-- Name: EscalaServidor PK_EscalaServidor; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaServidor"
    ADD CONSTRAINT "PK_EscalaServidor" PRIMARY KEY ("Id");


--
-- Name: Nucleo PK_Nucleo; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Nucleo"
    ADD CONSTRAINT "PK_Nucleo" PRIMARY KEY ("Id");


--
-- Name: Perfil PK_Perfil; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Perfil"
    ADD CONSTRAINT "PK_Perfil" PRIMARY KEY ("Id");


--
-- Name: PerfilPermissao PK_PerfilPermissao; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PerfilPermissao"
    ADD CONSTRAINT "PK_PerfilPermissao" PRIMARY KEY ("PerfilId", "PermissaoId");


--
-- Name: Permissao PK_Permissao; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Permissao"
    ADD CONSTRAINT "PK_Permissao" PRIMARY KEY ("Id");


--
-- Name: Servidor PK_Servidor; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Servidor"
    ADD CONSTRAINT "PK_Servidor" PRIMARY KEY ("Id");


--
-- Name: Setor PK_Setor; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Setor"
    ADD CONSTRAINT "PK_Setor" PRIMARY KEY ("Id");


--
-- Name: SetorChefia PK_SetorChefia; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SetorChefia"
    ADD CONSTRAINT "PK_SetorChefia" PRIMARY KEY ("SetorId", "TipoChefia");


--
-- Name: TipoOcorrencia PK_TipoOcorrencia; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TipoOcorrencia"
    ADD CONSTRAINT "PK_TipoOcorrencia" PRIMARY KEY ("Codigo");


--
-- Name: Usuario PK_Usuario; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Usuario"
    ADD CONSTRAINT "PK_Usuario" PRIMARY KEY ("Id");


--
-- Name: UsuarioPerfil PK_UsuarioPerfil; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UsuarioPerfil"
    ADD CONSTRAINT "PK_UsuarioPerfil" PRIMARY KEY ("UsuarioId", "PerfilId");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_Cargo_Codigo; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Cargo_Codigo" ON public."Cargo" USING btree ("Codigo");


--
-- Name: IX_Cargo_Nome; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Cargo_Nome" ON public."Cargo" USING btree ("Nome");


--
-- Name: IX_EscalaJornada_EscalaServidorId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EscalaJornada_EscalaServidorId" ON public."EscalaJornada" USING btree ("EscalaServidorId");


--
-- Name: IX_EscalaJornada_TipoOcorrenciaCodigo; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EscalaJornada_TipoOcorrenciaCodigo" ON public."EscalaJornada" USING btree ("TipoOcorrenciaCodigo");


--
-- Name: IX_EscalaOcorrencia_Data; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EscalaOcorrencia_Data" ON public."EscalaOcorrencia" USING btree ("Data");


--
-- Name: IX_EscalaOcorrencia_EscalaJornadaId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EscalaOcorrencia_EscalaJornadaId" ON public."EscalaOcorrencia" USING btree ("EscalaJornadaId");


--
-- Name: IX_EscalaOcorrencia_EscalaServidorId_Data; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_EscalaOcorrencia_EscalaServidorId_Data" ON public."EscalaOcorrencia" USING btree ("EscalaServidorId", "Data");


--
-- Name: IX_EscalaOcorrencia_TipoOcorrenciaCodigo; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EscalaOcorrencia_TipoOcorrenciaCodigo" ON public."EscalaOcorrencia" USING btree ("TipoOcorrenciaCodigo");


--
-- Name: IX_EscalaServidor_CargoId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EscalaServidor_CargoId" ON public."EscalaServidor" USING btree ("CargoId");


--
-- Name: IX_EscalaServidor_EscalaId_ServidorId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_EscalaServidor_EscalaId_ServidorId" ON public."EscalaServidor" USING btree ("EscalaId", "ServidorId");


--
-- Name: IX_EscalaServidor_ServidorId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EscalaServidor_ServidorId" ON public."EscalaServidor" USING btree ("ServidorId");


--
-- Name: IX_Escala_SetorId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Escala_SetorId" ON public."Escala" USING btree ("SetorId");


--
-- Name: IX_Escala_SetorId_Ano_Mes; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Escala_SetorId_Ano_Mes" ON public."Escala" USING btree ("SetorId", "Ano", "Mes");


--
-- Name: IX_Escala_Status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Escala_Status" ON public."Escala" USING btree ("Status");


--
-- Name: IX_Nucleo_ChefeServidorId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Nucleo_ChefeServidorId" ON public."Nucleo" USING btree ("ChefeServidorId");


--
-- Name: IX_Nucleo_Nome; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Nucleo_Nome" ON public."Nucleo" USING btree ("Nome");


--
-- Name: IX_Nucleo_Sigla; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Nucleo_Sigla" ON public."Nucleo" USING btree ("Sigla");


--
-- Name: IX_PerfilPermissao_PermissaoId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PerfilPermissao_PermissaoId" ON public."PerfilPermissao" USING btree ("PermissaoId");


--
-- Name: IX_Perfil_Codigo; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Perfil_Codigo" ON public."Perfil" USING btree ("Codigo");


--
-- Name: IX_Perfil_Nome; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Perfil_Nome" ON public."Perfil" USING btree ("Nome");


--
-- Name: IX_Permissao_Codigo; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Permissao_Codigo" ON public."Permissao" USING btree ("Codigo");


--
-- Name: IX_Permissao_Modulo; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Permissao_Modulo" ON public."Permissao" USING btree ("Modulo");


--
-- Name: IX_Servidor_CargoId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Servidor_CargoId" ON public."Servidor" USING btree ("CargoId");


--
-- Name: IX_Servidor_Cpf; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Servidor_Cpf" ON public."Servidor" USING btree ("Cpf");


--
-- Name: IX_Servidor_Email; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Servidor_Email" ON public."Servidor" USING btree ("Email");


--
-- Name: IX_Servidor_Matricula; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Servidor_Matricula" ON public."Servidor" USING btree ("Matricula");


--
-- Name: IX_Servidor_SetorId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Servidor_SetorId" ON public."Servidor" USING btree ("SetorId");


--
-- Name: IX_Servidor_Status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Servidor_Status" ON public."Servidor" USING btree ("Status");


--
-- Name: IX_SetorChefia_ServidorId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SetorChefia_ServidorId" ON public."SetorChefia" USING btree ("ServidorId");


--
-- Name: IX_Setor_Nome; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Setor_Nome" ON public."Setor" USING btree ("Nome");


--
-- Name: IX_Setor_NucleoId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Setor_NucleoId" ON public."Setor" USING btree ("NucleoId");


--
-- Name: IX_Setor_Sigla; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Setor_Sigla" ON public."Setor" USING btree ("Sigla");


--
-- Name: IX_UsuarioPerfil_PerfilId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_UsuarioPerfil_PerfilId" ON public."UsuarioPerfil" USING btree ("PerfilId");


--
-- Name: IX_Usuario_Login; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Usuario_Login" ON public."Usuario" USING btree ("Login");


--
-- Name: IX_Usuario_ServidorId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Usuario_ServidorId" ON public."Usuario" USING btree ("ServidorId");


--
-- Name: EscalaJornada FK_EscalaJornada_EscalaServidor_EscalaServidorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaJornada"
    ADD CONSTRAINT "FK_EscalaJornada_EscalaServidor_EscalaServidorId" FOREIGN KEY ("EscalaServidorId") REFERENCES public."EscalaServidor"("Id") ON DELETE CASCADE;


--
-- Name: EscalaJornada FK_EscalaJornada_TipoOcorrencia_TipoOcorrenciaCodigo; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaJornada"
    ADD CONSTRAINT "FK_EscalaJornada_TipoOcorrencia_TipoOcorrenciaCodigo" FOREIGN KEY ("TipoOcorrenciaCodigo") REFERENCES public."TipoOcorrencia"("Codigo") ON DELETE RESTRICT;


--
-- Name: EscalaOcorrencia FK_EscalaOcorrencia_EscalaJornada_EscalaJornadaId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaOcorrencia"
    ADD CONSTRAINT "FK_EscalaOcorrencia_EscalaJornada_EscalaJornadaId" FOREIGN KEY ("EscalaJornadaId") REFERENCES public."EscalaJornada"("Id") ON DELETE SET NULL;


--
-- Name: EscalaOcorrencia FK_EscalaOcorrencia_EscalaServidor_EscalaServidorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaOcorrencia"
    ADD CONSTRAINT "FK_EscalaOcorrencia_EscalaServidor_EscalaServidorId" FOREIGN KEY ("EscalaServidorId") REFERENCES public."EscalaServidor"("Id") ON DELETE CASCADE;


--
-- Name: EscalaOcorrencia FK_EscalaOcorrencia_TipoOcorrencia_TipoOcorrenciaCodigo; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaOcorrencia"
    ADD CONSTRAINT "FK_EscalaOcorrencia_TipoOcorrencia_TipoOcorrenciaCodigo" FOREIGN KEY ("TipoOcorrenciaCodigo") REFERENCES public."TipoOcorrencia"("Codigo") ON DELETE RESTRICT;


--
-- Name: EscalaServidor FK_EscalaServidor_Cargo_CargoId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaServidor"
    ADD CONSTRAINT "FK_EscalaServidor_Cargo_CargoId" FOREIGN KEY ("CargoId") REFERENCES public."Cargo"("Id") ON DELETE RESTRICT;


--
-- Name: EscalaServidor FK_EscalaServidor_Escala_EscalaId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaServidor"
    ADD CONSTRAINT "FK_EscalaServidor_Escala_EscalaId" FOREIGN KEY ("EscalaId") REFERENCES public."Escala"("Id") ON DELETE CASCADE;


--
-- Name: EscalaServidor FK_EscalaServidor_Servidor_ServidorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaServidor"
    ADD CONSTRAINT "FK_EscalaServidor_Servidor_ServidorId" FOREIGN KEY ("ServidorId") REFERENCES public."Servidor"("Id") ON DELETE RESTRICT;


--
-- Name: Escala FK_Escala_Setor_SetorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Escala"
    ADD CONSTRAINT "FK_Escala_Setor_SetorId" FOREIGN KEY ("SetorId") REFERENCES public."Setor"("Id") ON DELETE RESTRICT;


--
-- Name: Nucleo FK_Nucleo_Servidor_ChefeServidorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Nucleo"
    ADD CONSTRAINT "FK_Nucleo_Servidor_ChefeServidorId" FOREIGN KEY ("ChefeServidorId") REFERENCES public."Servidor"("Id") ON DELETE SET NULL;


--
-- Name: PerfilPermissao FK_PerfilPermissao_Perfil_PerfilId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PerfilPermissao"
    ADD CONSTRAINT "FK_PerfilPermissao_Perfil_PerfilId" FOREIGN KEY ("PerfilId") REFERENCES public."Perfil"("Id") ON DELETE CASCADE;


--
-- Name: PerfilPermissao FK_PerfilPermissao_Permissao_PermissaoId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PerfilPermissao"
    ADD CONSTRAINT "FK_PerfilPermissao_Permissao_PermissaoId" FOREIGN KEY ("PermissaoId") REFERENCES public."Permissao"("Id") ON DELETE CASCADE;


--
-- Name: Servidor FK_Servidor_Cargo_CargoId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Servidor"
    ADD CONSTRAINT "FK_Servidor_Cargo_CargoId" FOREIGN KEY ("CargoId") REFERENCES public."Cargo"("Id") ON DELETE RESTRICT;


--
-- Name: Servidor FK_Servidor_Setor_SetorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Servidor"
    ADD CONSTRAINT "FK_Servidor_Setor_SetorId" FOREIGN KEY ("SetorId") REFERENCES public."Setor"("Id") ON DELETE RESTRICT;


--
-- Name: SetorChefia FK_SetorChefia_Servidor_ServidorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SetorChefia"
    ADD CONSTRAINT "FK_SetorChefia_Servidor_ServidorId" FOREIGN KEY ("ServidorId") REFERENCES public."Servidor"("Id") ON DELETE RESTRICT;


--
-- Name: SetorChefia FK_SetorChefia_Setor_SetorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SetorChefia"
    ADD CONSTRAINT "FK_SetorChefia_Setor_SetorId" FOREIGN KEY ("SetorId") REFERENCES public."Setor"("Id") ON DELETE CASCADE;


--
-- Name: Setor FK_Setor_Nucleo_NucleoId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Setor"
    ADD CONSTRAINT "FK_Setor_Nucleo_NucleoId" FOREIGN KEY ("NucleoId") REFERENCES public."Nucleo"("Id") ON DELETE RESTRICT;


--
-- Name: UsuarioPerfil FK_UsuarioPerfil_Perfil_PerfilId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UsuarioPerfil"
    ADD CONSTRAINT "FK_UsuarioPerfil_Perfil_PerfilId" FOREIGN KEY ("PerfilId") REFERENCES public."Perfil"("Id") ON DELETE RESTRICT;


--
-- Name: UsuarioPerfil FK_UsuarioPerfil_Usuario_UsuarioId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UsuarioPerfil"
    ADD CONSTRAINT "FK_UsuarioPerfil_Usuario_UsuarioId" FOREIGN KEY ("UsuarioId") REFERENCES public."Usuario"("Id") ON DELETE CASCADE;


--
-- Name: Usuario FK_Usuario_Servidor_ServidorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Usuario"
    ADD CONSTRAINT "FK_Usuario_Servidor_ServidorId" FOREIGN KEY ("ServidorId") REFERENCES public."Servidor"("Id") ON DELETE RESTRICT;


--
-- PostgreSQL database dump complete
--

