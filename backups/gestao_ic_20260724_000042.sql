--
-- PostgreSQL database dump
--

\restrict q3hbgGsEVxEbh36HfPL72yPZkyPFz1Dtm6xIkGaiDczIR7jZj2m4o9HV0GISnzh

-- Dumped from database version 17.10
-- Dumped by pg_dump version 17.10

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
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
-- Name: Afastamento; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Afastamento" (
    "Id" uuid NOT NULL,
    "ServidorId" uuid NOT NULL,
    "DataInicio" date NOT NULL,
    "DataFim" date NOT NULL,
    "TipoOcorrenciaCodigo" character varying(20) NOT NULL,
    "Observacao" character varying(1000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100),
    "Sei" character varying(100)
);


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
    "Mes" integer DEFAULT 0 NOT NULL,
    "TipoFuncionamento" character varying(40) DEFAULT ''::character varying NOT NULL,
    "PublicadaEm" timestamp with time zone,
    "PublicadaPor" character varying(100)
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
    "UpdatedBy" character varying(100),
    "DataInicioCiclo" date,
    "PadraoEscalaId" uuid
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
-- Name: PadraoEscala; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PadraoEscala" (
    "Id" uuid NOT NULL,
    "Codigo" character varying(40) NOT NULL,
    "Nome" character varying(150) NOT NULL,
    "TipoFuncionamento" character varying(40) NOT NULL,
    "TipoJornada" character varying(40) NOT NULL,
    "RecorrenciaTipo" character varying(40) NOT NULL,
    "DiasTrabalho" integer,
    "DiasFolga" integer,
    "DiasSemana" character varying(40),
    "TipoOcorrenciaTrabalho" character varying(10) NOT NULL,
    "TipoOcorrenciaFolga" character varying(10) NOT NULL,
    "HoraInicioPadrao" time without time zone,
    "HoraFimPadrao" time without time zone,
    "HorasPadrao" numeric(5,2),
    "Sistema" boolean NOT NULL,
    "Ativo" boolean NOT NULL,
    "SetorId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100)
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
    "UpdatedBy" character varying(100),
    "Area" character varying(80) DEFAULT ''::character varying NOT NULL
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
-- Name: SolicitacaoDevolucaoEscala; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SolicitacaoDevolucaoEscala" (
    "Id" uuid NOT NULL,
    "EscalaId" uuid NOT NULL,
    "SolicitanteUsuarioId" uuid NOT NULL,
    "Justificativa" character varying(2000) NOT NULL,
    "Status" character varying(40) NOT NULL,
    "RespondidoPor" character varying(100),
    "RespostaEm" timestamp with time zone,
    "ObservacaoResposta" character varying(2000),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" character varying(100),
    "UpdatedBy" character varying(100)
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
    "UpdatedBy" character varying(100),
    "DeveAlterarSenha" boolean DEFAULT false NOT NULL
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
-- Data for Name: Afastamento; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Afastamento" ("Id", "ServidorId", "DataInicio", "DataFim", "TipoOcorrenciaCodigo", "Observacao", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "Sei") FROM stdin;
dc2185ba-3216-4382-9cac-4573596e8dca	22222222-2222-2222-2222-222222222222	2026-11-11	2026-11-17	LM	Atestado SEI 03900012212.123123 editei ta erradosddd	2026-07-23 17:01:32.822877+00	2026-07-23 21:36:45.878088+00	vitorlopes	123	\N
03c5d192-935c-43d4-ada8-df7e776daec2	c3baf373-ff8b-4635-af22-9934234cbd92	2026-10-13	2026-10-20	FR	sadas	2026-07-23 14:45:06.336604+00	2026-07-23 23:25:01.885404+00	vitorlopes	123	\N
168c72f2-17c1-4557-b315-97bdfeb85748	b2536338-d43c-431f-b516-4cd524c3d78f	2026-08-01	2026-08-01	LM	etestestestesbfuiweabfuiewbfuiewfuiwufuiwbfuwbuibiiiiiiiiiiiiiiiiiiiiiiiiiiiifnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn	2026-07-24 02:07:48.160817+00	\N	123	\N	ghdhdthtdhdthdthdthdthdthtdh
\.


--
-- Data for Name: Cargo; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Cargo" ("Id", "Nome", "Codigo", "Ativo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
174cf381-fb97-4301-aa54-227cbc179b23	Perito Criminal	PC	t	2026-07-21 22:33:07.290765+00	\N	migration	\N
9b80a4b6-4a6c-43c1-a08c-a30e1814644b	Agente Técnico Forense	ATF	t	2026-07-21 22:33:07.290765+00	\N	migration	\N
df9ad520-3051-4d7e-9f54-d43f7c13fc3e	Agente de Necrópsia	AN	t	2026-07-21 22:33:07.290765+00	\N	migration	\N
76470c0b-7c46-456f-8c10-053e75a92358	Assistente Técnico Forense	ASTF	t	2026-07-21 22:33:07.290765+00	\N	migration	\N
61b2394f-bdb8-4344-888c-e78e01a7f5e6	Estagiário	EST	t	2026-07-21 22:33:07.290765+00	\N	migration	\N
86ebd46f-50f1-483b-b8ee-e84ae7d8205a	Terceirizado	TER	t	2026-07-21 22:33:07.290765+00	\N	migration	\N
661d799f-4307-463e-9215-dd84698c5d98	Servidor Externo	EXT	t	2026-07-21 22:33:07.290765+00	\N	migration	\N
0b0c4776-467c-6f45-8c10-053e75a92358	Assistente Técnico Forense	ASSISTENTE_TECNICO_FORENSE	f	2026-07-23 19:17:41.983453+00	2026-07-23 20:11:43.918839+00	seed	seed
20d59adf-5130-7e4d-9f54-d43f7c13fc3e	Agente de Necrópsia	AGENTE_NECROPSIA	f	2026-07-23 19:17:41.983425+00	2026-07-23 20:11:43.919982+00	seed	seed
4f39b261-b8bd-4443-888c-e78e01a7f5e6	Estagiário	ESTAGIARIO	f	2026-07-23 19:17:41.983482+00	2026-07-23 20:11:43.920925+00	seed	seed
6fd4eb86-f150-3b48-b8ee-e84ae7d8205a	Terceirizado	TERCEIRIZADO	f	2026-07-23 19:17:41.983499+00	2026-07-23 20:11:43.921871+00	seed	seed
81f34c17-97fb-0143-aa54-227cbc179b23	Perito Criminal	PERITO_CRIMINAL	f	2026-07-23 19:17:41.963126+00	2026-07-23 20:11:43.922759+00	seed	seed
9f791d66-0743-3e46-9215-dd84698c5d98	Servidor Externo	SERVIDOR_EXTERNO	f	2026-07-23 19:17:41.983514+00	2026-07-23 20:11:43.923662+00	seed	seed
b6a4809b-6c4a-c143-a08c-a30e1814644b	Agente Técnico Forense	AGENTE_TECNICO_FORENSE	f	2026-07-23 19:17:41.983004+00	2026-07-23 20:11:43.924645+00	seed	seed
\.


--
-- Data for Name: Escala; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Escala" ("Id", "SetorId", "Status", "Observacao", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "Ano", "Mes", "TipoFuncionamento", "PublicadaEm", "PublicadaPor") FROM stdin;
26666fe7-1db5-44e9-ba59-f44b2244821c	11111111-1111-1111-1111-111111111111	Rascunho	\N	2026-07-22 01:57:38.303557+00	\N	vitorlopes	\N	2026	8		\N	\N
436591f8-6d78-4cdb-9453-e1b17dfab17a	11111111-1111-1111-1111-111111111111	Rascunho	\N	2026-07-23 12:32:19.053763+00	\N	vitorlopes	\N	2026	9	0	\N	\N
ac95eee5-f4d3-4ca2-bf0f-221bbf8a6f93	11111111-1111-1111-1111-111111111111	Rascunho	\N	2026-07-23 12:35:47.854483+00	\N	vitorlopes	\N	2026	10	VinteQuatroHoras	\N	\N
4cfc9da6-8e52-4106-872c-f5f9b84d745a	11111111-1111-1111-1111-111111111111	Publicada	\N	2026-07-23 16:12:02.451501+00	2026-07-23 19:42:56.13502+00	vitorlopes	vitorlopes	2026	11	VinteQuatroHoras	2026-07-23 19:42:56.134984+00	vitorlopes
9f8fc8cc-819c-4c4f-8535-20e84d5385fd	dcabe2de-a1c5-49e8-b10e-3a20fa740f78	Finalizada	\N	2026-07-23 22:06:11.825972+00	2026-07-24 00:35:16.026614+00	123	vitorlopes	2026	8	Expediente	\N	\N
\.


--
-- Data for Name: EscalaJornada; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."EscalaJornada" ("Id", "EscalaServidorId", "TipoJornada", "DataInicio", "DataFim", "HoraInicio", "HoraFim", "Horas", "TipoOcorrenciaCodigo", "RecorrenciaTipo", "DiasSemana", "IntervaloDias", "DiasTrabalho", "DiasFolga", "TipoOcorrenciaFolgaCodigo", "Observacao", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "DataInicioCiclo", "PadraoEscalaId") FROM stdin;
ce21ae9c-5fc6-404d-b09e-09de88b9dce4	99328c91-43dc-44e4-94cb-306d64fa7795	Plantao	2026-08-01	2026-08-31	07:00:00	19:00:00	12.00	PD	CicloPlantao	\N	\N	1	1	D	\N	2026-07-22 03:52:01.657053+00	\N	vitorlopes	\N	2026-08-01	1e025d54-2bda-4be4-9139-ebd0b096251c
748d5958-8b76-40c0-acb1-4862becbb7e1	61f6cf54-d678-474b-8a0d-c042ec64ec82	Plantao	2026-08-01	2026-08-31	07:00:00	19:00:00	12.00	PD	CicloPlantao	\N	\N	1	1	D	\N	2026-07-22 03:52:01.717607+00	\N	vitorlopes	\N	2026-08-02	1e025d54-2bda-4be4-9139-ebd0b096251c
08aa908e-9597-49d0-88f8-a1f429b9b06a	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	Plantao	2026-08-01	2026-08-31	07:00:00	19:00:00	12.00	PD	CicloPlantao	\N	\N	1	1	D	\N	2026-07-22 03:52:01.72827+00	\N	vitorlopes	\N	2026-08-03	1e025d54-2bda-4be4-9139-ebd0b096251c
3f257727-b05d-4649-8ec7-853582b614bf	383d9e62-67d1-4e02-ba26-abb51f432c68	Plantao	2026-09-01	2026-09-30	07:00:00	19:00:00	12.00	PD	CicloPlantao	\N	\N	1	1	D	\N	2026-07-23 12:32:19.102421+00	\N	vitorlopes	\N	2026-08-01	1e025d54-2bda-4be4-9139-ebd0b096251c
caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	6f343a4b-3041-4b82-a1c7-9b2129f33d93	Plantao	2026-09-01	2026-09-30	07:00:00	19:00:00	12.00	PD	CicloPlantao	\N	\N	1	1	D	\N	2026-07-23 12:32:19.114232+00	\N	vitorlopes	\N	2026-08-02	1e025d54-2bda-4be4-9139-ebd0b096251c
3672323a-1f45-480d-b073-9e98fb33f9ca	cc355bda-2f4a-4044-99c4-2a6447e96d44	Plantao	2026-09-01	2026-09-30	07:00:00	19:00:00	12.00	PD	CicloPlantao	\N	\N	1	1	D	\N	2026-07-23 12:32:19.123247+00	\N	vitorlopes	\N	2026-08-03	1e025d54-2bda-4be4-9139-ebd0b096251c
a2fea367-0c4c-4720-a4b2-021a44819151	d03d5db6-362d-488c-aebc-fc171c8d3a6d	Plantao	2026-10-01	2026-10-31	07:00:00	07:00:00	24.00	PT	CicloPlantao	\N	\N	1	3	D	\N	2026-07-23 12:35:47.97987+00	\N	vitorlopes	\N	2026-10-02	5cc40f20-e248-4668-8849-0b98c736f22d
8955535c-bfda-4619-a0a3-edaa7bb7cd1f	5288a194-90a6-48c1-923a-ee7d3451ccd4	Plantao	2026-10-01	2026-10-31	07:00:00	07:00:00	24.00	PT	CicloPlantao	\N	\N	1	3	D	\N	2026-07-23 12:35:47.987458+00	\N	vitorlopes	\N	2026-10-03	5cc40f20-e248-4668-8849-0b98c736f22d
0b94c93e-fb8f-424b-9e8b-b43adf5208e0	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	Plantao	2026-11-01	2026-11-30	07:00:00	19:00:00	12.00	PD	CicloPlantao	\N	\N	1	1	D	\N	2026-07-23 19:41:39.964604+00	\N	vitorlopes	\N	2026-11-02	1e025d54-2bda-4be4-9139-ebd0b096251c
ace48173-19d3-4e78-acfe-ecda3c235e7c	5c209eaf-0043-41f8-9324-d8edbee5704e	Plantao	2026-11-01	2026-11-30	07:00:00	19:00:00	12.00	PD	CicloPlantao	\N	\N	1	1	D	\N	2026-07-23 19:41:39.969002+00	\N	vitorlopes	\N	2026-11-03	1e025d54-2bda-4be4-9139-ebd0b096251c
0abf5f83-3086-4f1c-af5c-f1677abceaef	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	Plantao	2026-11-01	2026-11-30	07:00:00	19:00:00	12.00	PD	CicloPlantao	\N	\N	1	1	D	\N	2026-07-23 19:41:39.971779+00	\N	vitorlopes	\N	2026-11-03	1e025d54-2bda-4be4-9139-ebd0b096251c
46f1f9c1-5f03-43c5-81dd-00b6689f5ec2	71dfdc78-93aa-48a3-beef-d809cce85334	Expediente	2026-08-01	2026-08-31	08:00:00	14:00:00	6.00	M	DiasSemana	1,2,3,4,5	\N	\N	\N	D	\N	2026-07-23 22:42:43.542577+00	\N	123	\N	2026-08-01	ed5caa85-39fe-45a9-a35b-9fab3cfa5121
078be9ac-d7c1-4558-8dc5-0793f083d6fb	4e95ec87-4478-4d4c-84c1-fc5daa459382	Expediente	2026-08-01	2026-08-31	08:00:00	14:00:00	6.00	M	DiasSemana	1,2,3,4,5	\N	\N	\N	D	\N	2026-07-23 22:42:43.554084+00	\N	123	\N	2026-08-01	ed5caa85-39fe-45a9-a35b-9fab3cfa5121
\.


--
-- Data for Name: EscalaOcorrencia; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."EscalaOcorrencia" ("Id", "EscalaServidorId", "Data", "TipoOcorrenciaCodigo", "HoraInicio", "HoraFim", "Horas", "Origem", "EscalaJornadaId", "Observacao", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
17a3f87f-7608-4230-900f-704ffcd02071	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-14	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106964+00	\N	vitorlopes	\N
1cb21367-515c-47c4-afff-4f24dc1d426f	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-23	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107192+00	\N	vitorlopes	\N
2d980891-fe83-4e58-82fb-4eafe22f8890	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-30	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107274+00	\N	vitorlopes	\N
3776ddae-158b-4fdc-98bd-d08bf879d548	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-11	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106936+00	\N	vitorlopes	\N
406159ad-9195-4935-91d0-f097b9d6e522	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-01	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106696+00	\N	vitorlopes	\N
4be1bd4e-626d-484d-b705-22a83666fef7	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-03	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106846+00	\N	vitorlopes	\N
5b6f05bc-5da5-4315-90a1-2d6d4850ca7f	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-18	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106999+00	\N	vitorlopes	\N
5bd8eea2-cbed-47a0-8b7e-0fffe83b1ff7	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-02	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.10682+00	\N	vitorlopes	\N
5d5ea5e4-7d3d-4f3e-8bba-2ac012c6cd10	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-17	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.10699+00	\N	vitorlopes	\N
6b26a952-68c9-4ce5-9b81-4b113f99bf9a	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-09	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106918+00	\N	vitorlopes	\N
74a648f0-30ee-4fdd-a7b3-ab1daae969a9	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-15	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106972+00	\N	vitorlopes	\N
7c41744b-d0d1-46d0-80e9-2486d28e82f7	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-24	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107208+00	\N	vitorlopes	\N
842f24ff-0f48-402e-b3e0-d813c123dd70	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-20	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107018+00	\N	vitorlopes	\N
896f5ba7-41b4-42f6-882e-c7d96add4e09	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-13	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106956+00	\N	vitorlopes	\N
8ba1f34f-8a7b-4489-a338-d6580d92106e	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-10	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106928+00	\N	vitorlopes	\N
9cf4fd62-84de-4535-91f1-a1b9c5796782	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-21	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107027+00	\N	vitorlopes	\N
a1762f6f-3582-4e95-9c75-3eb92447d1db	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-07	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106894+00	\N	vitorlopes	\N
a32f95bc-3084-4f9e-9c38-a2e54d7d35fa	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-26	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107229+00	\N	vitorlopes	\N
ab4505dc-cdd8-4e6a-8ee6-230665885875	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-06	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106885+00	\N	vitorlopes	\N
b367a1fb-f208-48a6-8ad6-9909b0e65099	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-05	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106872+00	\N	vitorlopes	\N
c366cd58-4671-4633-b4da-c3ee798c10e5	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-22	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107038+00	\N	vitorlopes	\N
c7ad4454-3003-4911-8184-4c51a4ff9639	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-16	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106981+00	\N	vitorlopes	\N
ca01d572-fe41-412d-b088-9787542511d6	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-28	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107253+00	\N	vitorlopes	\N
caf416ef-095a-4e12-b771-31f359d1b6a6	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-25	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.10722+00	\N	vitorlopes	\N
d63c2b6d-2310-4269-8be6-fd0808a3c913	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-08	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106903+00	\N	vitorlopes	\N
dbdd2aff-9e04-4ef0-bda8-68be0ddeff21	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-27	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107241+00	\N	vitorlopes	\N
df442224-ed22-486a-b9f7-9069bb43f936	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-19	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107009+00	\N	vitorlopes	\N
f870dee6-d907-4a4d-8165-4e9a2f223912	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-12	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106947+00	\N	vitorlopes	\N
fa8012a1-2143-41bd-8146-c45bcb8bf7e7	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-04	PD	07:00:00	19:00:00	12.00	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.106858+00	\N	vitorlopes	\N
fea41972-f254-4b46-9570-43ccf385e3bf	383d9e62-67d1-4e02-ba26-abb51f432c68	2026-09-29	D	\N	\N	\N	Regra	3f257727-b05d-4649-8ec7-853582b614bf	\N	2026-07-23 12:32:19.107264+00	\N	vitorlopes	\N
02b36565-61c3-45b5-a4be-8058e3c7385b	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-17	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.11649+00	\N	vitorlopes	\N
04728539-4e79-4a89-89c7-9927529a7132	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-30	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116622+00	\N	vitorlopes	\N
0f3149fe-c2b3-446c-8406-a97164fde415	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-18	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116511+00	\N	vitorlopes	\N
101e0317-8f0e-4c11-b4b5-2a0f8cdf043c	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-25	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116577+00	\N	vitorlopes	\N
1e205511-ccfe-4dd0-8566-6b93985dbf7a	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-07	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116391+00	\N	vitorlopes	\N
2834279c-94c8-482b-b96b-efa4305d5a2f	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-19	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116523+00	\N	vitorlopes	\N
28ff09d1-0f68-4fc6-b1ac-094bb1b81fc0	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-29	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116613+00	\N	vitorlopes	\N
2e9b72b2-817f-4695-9606-685041776291	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-14	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116454+00	\N	vitorlopes	\N
40d8e2f4-f129-4887-8a60-ec7e57c15ab1	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-02	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116339+00	\N	vitorlopes	\N
48adb1fe-4a59-4fa6-88af-f6cf9a2b4e8a	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-11	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116427+00	\N	vitorlopes	\N
4bda3359-ff96-4a45-a64a-1b410e7252c7	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-12	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116437+00	\N	vitorlopes	\N
4e6356e2-b1de-4e73-b5c5-27c51cb00681	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-15	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116463+00	\N	vitorlopes	\N
5114de53-eb0e-43ed-a97d-70baaa3beb84	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-04	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116361+00	\N	vitorlopes	\N
518c108d-5005-470a-a9fd-0dea82a24bfd	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-21	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116542+00	\N	vitorlopes	\N
55c2ceaa-5caa-4169-a736-4e98a537e3f5	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-27	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116595+00	\N	vitorlopes	\N
5bcc27d0-8783-480a-92ec-30690770f74c	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-23	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.11656+00	\N	vitorlopes	\N
601d87a1-1e23-497f-9457-7fe51fc647c7	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-22	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.11655+00	\N	vitorlopes	\N
6483c4a3-9523-4955-ad4c-4c1f5b417f5d	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-20	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116533+00	\N	vitorlopes	\N
770c070e-8df5-4673-bce6-ab58e4e36e5d	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-28	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116604+00	\N	vitorlopes	\N
9b0025aa-370a-458a-8afc-5792c2393923	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-16	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116471+00	\N	vitorlopes	\N
b3b8973d-95c0-43f2-98d1-04e6ad1c0b63	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-09	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.11641+00	\N	vitorlopes	\N
b59afd6b-9b7b-401f-b449-8cb8184b51d5	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-13	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116446+00	\N	vitorlopes	\N
b740f99c-a961-4497-9a4b-b6f936b8330e	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-26	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116586+00	\N	vitorlopes	\N
bc041350-404a-4b45-ad80-0c54d3cc3325	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-24	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116569+00	\N	vitorlopes	\N
d5fccb47-7551-4b9b-a04e-eb395586fb2d	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-08	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116399+00	\N	vitorlopes	\N
dad9100e-3e93-49fe-a36e-2fa61770c9ac	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-05	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116371+00	\N	vitorlopes	\N
dced6336-03db-49a2-84bd-839e796badf8	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-10	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116419+00	\N	vitorlopes	\N
f1a42cf8-a3db-415f-bdcf-7ecbe498c1c0	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-03	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116352+00	\N	vitorlopes	\N
f4c2d7d4-537e-4907-a5fa-23d8db0c0385	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-06	D	\N	\N	\N	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116382+00	\N	vitorlopes	\N
f62168c5-9ea0-4441-bdbe-ac2266269ab8	6f343a4b-3041-4b82-a1c7-9b2129f33d93	2026-09-01	PD	07:00:00	19:00:00	12.00	Regra	caaebc52-14fa-4ef5-9d45-c3c0bc2a0cc6	\N	2026-07-23 12:32:19.116302+00	\N	vitorlopes	\N
0737efbd-dcff-4552-a749-798b49244e1d	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-24	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125846+00	\N	vitorlopes	\N
17da9791-6d4b-4897-9ae1-3b38fa0256f4	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-03	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668672+00	\N	vitorlopes	\N
1961fe50-ab7b-4db0-83d2-67075a1316ac	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-14	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668747+00	\N	vitorlopes	\N
2b891b55-3cec-4f26-b687-eb2072db0563	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-28	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668853+00	\N	vitorlopes	\N
3fe9c47c-1d67-442c-9a32-b0e8c193ac06	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-10	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668725+00	\N	vitorlopes	\N
47b3c322-6066-4ae5-bdae-4b8fb2608c34	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-09	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.66872+00	\N	vitorlopes	\N
4ed4fff0-52a9-4052-9ba8-b29c6e302644	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-08	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668715+00	\N	vitorlopes	\N
519d2fc2-99cc-41c3-9f6c-23a97b0c766f	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-19	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668796+00	\N	vitorlopes	\N
5d661e13-f9c4-48f8-93dc-19be9c7a8dd0	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-05	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668694+00	\N	vitorlopes	\N
5ebe8b26-6f9d-45b6-8aee-ee8f1bf595da	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-23	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668824+00	\N	vitorlopes	\N
6e9b8828-3f76-48f0-ab43-626680340db0	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-29	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668885+00	\N	vitorlopes	\N
72a97bc6-d59f-4b35-b89a-755a95a2fc16	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-04	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668688+00	\N	vitorlopes	\N
7b58b1cd-7af6-462e-8e67-9900ce13e22d	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-07	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668704+00	\N	vitorlopes	\N
835e5165-f789-4639-8e16-2c0a98e4486d	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-01	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668612+00	\N	vitorlopes	\N
85845760-da67-46f3-93db-fc4aea44e782	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-26	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668837+00	\N	vitorlopes	\N
93a8aea3-6fca-4e6a-90c2-b7afc8110720	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-21	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668806+00	\N	vitorlopes	\N
93c7380b-2684-4bc4-bc57-9e069d74ff3a	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-11	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668733+00	\N	vitorlopes	\N
995171c2-3b4f-44e2-a651-f522d31c5830	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-20	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668801+00	\N	vitorlopes	\N
ae78a98e-8730-4440-8128-7c5cd56199ca	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-24	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668829+00	\N	vitorlopes	\N
c2663f8c-f9b2-4c7b-ae9c-60bad0ba96be	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-17	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668778+00	\N	vitorlopes	\N
c33ebaff-cef2-4d42-9b46-fbfafaa55507	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-16	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668773+00	\N	vitorlopes	\N
c4365ec1-3071-41df-9b25-cae8f5f7c6c8	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-15	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668767+00	\N	vitorlopes	\N
c925ff0a-dbc8-46c3-83ba-f0bcc15e7f70	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-22	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668819+00	\N	vitorlopes	\N
cf77ae30-120a-4bc9-8a9c-45c59f4c6ae5	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-27	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668848+00	\N	vitorlopes	\N
d7da7b05-2e72-49a6-8de3-0ebcdafd1996	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-06	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668699+00	\N	vitorlopes	\N
dcfd6045-69ce-4d81-8236-dfac74dc4284	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-18	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668782+00	\N	vitorlopes	\N
ed82d949-7e61-417c-948e-e10553d99379	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-13	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668743+00	\N	vitorlopes	\N
ee36fbd3-92bf-4c34-b008-c9e8103fd2ed	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-02	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668665+00	\N	vitorlopes	\N
f2b0e0b4-3888-4102-b5ec-e255e6bc3626	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-30	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668899+00	\N	vitorlopes	\N
f728e738-2ea7-4f44-940d-17a57b48f470	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-25	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668833+00	\N	vitorlopes	\N
f9a7acf3-5b46-4d43-a03e-5d08a2f34d8a	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-31	PD	07:00:00	19:00:00	12.00	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668913+00	\N	vitorlopes	\N
fbf72ca2-6af9-41c2-ab0c-679ba2863cdc	99328c91-43dc-44e4-94cb-306d64fa7795	2026-08-12	D	\N	\N	\N	Regra	ce21ae9c-5fc6-404d-b09e-09de88b9dce4	\N	2026-07-22 03:52:01.668738+00	\N	vitorlopes	\N
00f37e8b-913e-4556-87a9-da7c2c223a90	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-06	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727784+00	\N	vitorlopes	\N
04788c4d-f273-43bd-9dd2-d1e87c469b05	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-26	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.72788+00	\N	vitorlopes	\N
196014d3-43c8-4835-a6ab-0d3a8cb3e132	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-29	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727893+00	\N	vitorlopes	\N
1a3345f1-83e0-450c-886e-e2b4ae32aeb3	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-07	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727788+00	\N	vitorlopes	\N
1f7c67d2-174d-42e1-941b-3c035c37994f	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-11	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727809+00	\N	vitorlopes	\N
2c7f2565-2760-420d-b895-0447540f0922	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-02	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727759+00	\N	vitorlopes	\N
2efc98e0-57d7-4144-a55e-e01e0b288cdb	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-01	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727726+00	\N	vitorlopes	\N
30edb989-6cb7-4a07-b405-cb12c0558b41	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-20	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727851+00	\N	vitorlopes	\N
3da3eb4e-81b5-46de-9c52-a59004a81bf7	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-30	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727898+00	\N	vitorlopes	\N
487e3bd3-c74d-4bff-bd41-7192876f1842	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-12	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727813+00	\N	vitorlopes	\N
4e7a9bf5-36b7-41fe-a7a2-7d0f8faf3875	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-10	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727804+00	\N	vitorlopes	\N
4f93290b-86f4-46b2-836a-965b9d1e1770	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-15	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727827+00	\N	vitorlopes	\N
5c059475-6593-4eaa-ae9e-cf95a3c83ed7	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-24	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.72787+00	\N	vitorlopes	\N
5e3192ac-6d62-42d8-b2ed-435c928224f9	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-27	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727884+00	\N	vitorlopes	\N
64296153-34b5-4bae-bd2f-35b36bf91e44	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-31	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727903+00	\N	vitorlopes	\N
6b6a5a62-b61e-4d83-983c-a71cf5d75147	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-25	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727875+00	\N	vitorlopes	\N
72e40a97-b893-4b2e-a75d-7a0956e981c5	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-09	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727799+00	\N	vitorlopes	\N
83cde5bb-c39a-4c4e-b6ad-deb8cb42245a	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-28	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727889+00	\N	vitorlopes	\N
961795be-cd20-46ec-a67a-771092c73597	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-16	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727832+00	\N	vitorlopes	\N
ac5c936d-21bc-4652-9174-18c9e064e0cb	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-08	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727793+00	\N	vitorlopes	\N
b21c188e-dc68-4e06-a8d8-fbb21014d33f	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-13	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727818+00	\N	vitorlopes	\N
b23285f4-5409-4c37-9f14-87df9e6d9c28	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-23	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727866+00	\N	vitorlopes	\N
b90ac615-acc8-4027-b5d0-3fa228fe26cc	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-05	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727778+00	\N	vitorlopes	\N
d2e65668-5668-4d09-acdd-59ef77986bec	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-03	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727767+00	\N	vitorlopes	\N
d9d20dce-4ca5-47c2-8631-e44996f37093	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-14	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727823+00	\N	vitorlopes	\N
df4bccef-33d1-48ed-bae9-f8161cff50c5	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-18	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727842+00	\N	vitorlopes	\N
e33a9678-227c-4d40-8b40-01bbcf01f1d0	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-22	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727861+00	\N	vitorlopes	\N
e89cbbc1-4036-401a-b2bb-326a15a06c64	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-19	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727847+00	\N	vitorlopes	\N
ec9d2887-849f-483e-8d3d-c7ff7d7216ea	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-17	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727836+00	\N	vitorlopes	\N
eeaa05c4-237a-4fa5-a4ca-44642bf9cbc0	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-21	D	\N	\N	\N	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727856+00	\N	vitorlopes	\N
f744060d-7aaf-469b-a033-da28dd454e63	61f6cf54-d678-474b-8a0d-c042ec64ec82	2026-08-04	PD	07:00:00	19:00:00	12.00	Regra	748d5958-8b76-40c0-acb1-4862becbb7e1	\N	2026-07-22 03:52:01.727772+00	\N	vitorlopes	\N
0a3be1cb-4e43-474d-a867-3506cbcd763a	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-26	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738889+00	\N	vitorlopes	\N
0f027368-c529-4345-b82f-4e69c3b92b70	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-01	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738742+00	\N	vitorlopes	\N
28fe7247-80b5-4be4-8009-cbff3dda84e1	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-11	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738816+00	\N	vitorlopes	\N
2b095f5b-4f41-4f92-8510-645aef4a1f27	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-19	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738855+00	\N	vitorlopes	\N
329b877e-5297-48ff-8022-c8699f408f2a	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-23	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738874+00	\N	vitorlopes	\N
3c153658-a082-42c4-84f1-042d06b47b68	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-18	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.73885+00	\N	vitorlopes	\N
41ab1bc3-c201-4dbf-9e9a-7002c68bd43f	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-22	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738869+00	\N	vitorlopes	\N
4892b5d2-7345-4fe1-81a4-894199e94981	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-14	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.73883+00	\N	vitorlopes	\N
503fa3fb-1c6a-4564-8bea-d8b59a53a140	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-20	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.73886+00	\N	vitorlopes	\N
56fc5e62-0893-4e33-beee-4899d4716426	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-02	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738766+00	\N	vitorlopes	\N
5a7255de-d7fe-4cee-9d03-de1ba554a59d	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-15	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738835+00	\N	vitorlopes	\N
79ab7264-1a13-4ab4-b283-cd6763013ed5	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-16	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.73884+00	\N	vitorlopes	\N
834fa279-6bd3-4d98-8bbf-5030c3b1105b	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-12	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738821+00	\N	vitorlopes	\N
86d8f3ba-c292-491b-80ae-cfe5fdb5abd4	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-30	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738907+00	\N	vitorlopes	\N
8d4bf3d8-0bce-46de-82a3-59ec89aa4db0	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-29	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738903+00	\N	vitorlopes	\N
9a130892-855c-44ce-86fd-0b0b8ec0f0f4	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-04	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738781+00	\N	vitorlopes	\N
9b62ac51-fbbe-410a-9df1-3a3f7ae23e6b	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-24	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738879+00	\N	vitorlopes	\N
a20dbb87-ec20-44b5-a45e-75877c37ee18	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-28	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738898+00	\N	vitorlopes	\N
a7284ffc-039f-4d9d-847b-ef8953e0b68c	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-17	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738845+00	\N	vitorlopes	\N
aee66d69-9038-4c9f-bdf2-91f2a5763ef8	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-03	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738773+00	\N	vitorlopes	\N
ba070efb-09bb-4d31-a94a-11187d47c95a	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-06	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738791+00	\N	vitorlopes	\N
bc141fe3-9fd0-4758-9065-7d8771420e12	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-13	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738826+00	\N	vitorlopes	\N
bcdb06fd-a071-46ef-96fd-08dd67f69bf9	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-21	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738865+00	\N	vitorlopes	\N
c39ce327-ab72-4bf0-bf3a-94644c01d3c1	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-09	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738807+00	\N	vitorlopes	\N
c40991e3-db71-4d3d-8826-67a3f518dd98	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-27	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738893+00	\N	vitorlopes	\N
c562958f-abbd-44d0-985c-56e2d9ed3205	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-25	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738884+00	\N	vitorlopes	\N
c638e0ab-e065-4bb2-83c1-9a184ca00c70	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-05	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738786+00	\N	vitorlopes	\N
c922e5b3-74fb-4496-b85b-c74645df98d6	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-08	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738801+00	\N	vitorlopes	\N
d9a27959-2a33-423a-ad05-87c2359b3dcc	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-07	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738796+00	\N	vitorlopes	\N
e3af0f43-7baf-45f7-be6d-52717d4d3154	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-10	D	\N	\N	\N	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738811+00	\N	vitorlopes	\N
fd89fc67-0785-4e07-9e01-71f0ae1a166d	d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	2026-08-31	PD	07:00:00	19:00:00	12.00	Regra	08aa908e-9597-49d0-88f8-a1f429b9b06a	\N	2026-07-22 03:52:01.738912+00	\N	vitorlopes	\N
12272a82-d984-4ae3-9cff-6af8cb8c8c64	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-28	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125889+00	\N	vitorlopes	\N
1295b355-dbb4-4972-9d5d-b58fe0fcef1f	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-14	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125737+00	\N	vitorlopes	\N
1649afb2-58e7-44f7-bc92-5db41f42d8cf	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-21	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125814+00	\N	vitorlopes	\N
1892572a-5045-405e-abb0-28071f269ba3	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-06	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.12565+00	\N	vitorlopes	\N
304cc3b0-9382-439d-8751-2af1b975f20d	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-11	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125706+00	\N	vitorlopes	\N
36d7e74b-06f8-430a-94a7-b03d250c01b0	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-23	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125836+00	\N	vitorlopes	\N
40304d7d-569f-4144-b1c4-2832d432fbeb	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-03	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125615+00	\N	vitorlopes	\N
41a26be5-0340-45ad-b044-d236c20a4c13	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-18	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.12578+00	\N	vitorlopes	\N
422d5674-97a9-42df-91cc-d9a293f04b82	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-02	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125597+00	\N	vitorlopes	\N
551217aa-09d2-4fbd-a967-3d1ca4cbed9d	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-08	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125673+00	\N	vitorlopes	\N
6c729a69-294b-4f1b-af32-ddcbc6eccd87	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-29	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125899+00	\N	vitorlopes	\N
712b794f-02cb-4119-bae4-4d0be0788cc1	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-30	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125911+00	\N	vitorlopes	\N
7b31a81e-4259-4185-b3f8-de9b482ce3f2	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-20	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125803+00	\N	vitorlopes	\N
7d051fd1-7df1-4f5a-9751-41ec4fe5f3d6	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-19	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125793+00	\N	vitorlopes	\N
8baf6b51-da0a-4c68-90ab-c39257ab8880	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-15	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125749+00	\N	vitorlopes	\N
92e92a51-02f7-4153-8ad7-f1a71a834e9c	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-27	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125879+00	\N	vitorlopes	\N
a89fabd0-c97a-43be-ab14-14a27a3eb17f	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-10	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125695+00	\N	vitorlopes	\N
a9743c71-2f8c-4d9b-a719-a397c8b5fda5	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-25	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125857+00	\N	vitorlopes	\N
bdc5434d-d70f-419c-a9c0-6d08f846a688	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-07	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125662+00	\N	vitorlopes	\N
cbde0527-a08d-4b69-97e7-3260b312c2f4	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-01	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125544+00	\N	vitorlopes	\N
cfe92c7e-0bbb-46e4-9a3d-3497959cd9de	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-22	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125826+00	\N	vitorlopes	\N
d2629ff8-ebb0-405a-8564-9ced2d23476f	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-16	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125759+00	\N	vitorlopes	\N
d4b1e2ad-0941-4eaf-a30e-207b56c16707	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-17	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.12577+00	\N	vitorlopes	\N
d5cc9b8b-222f-45ff-83be-cfcc4b173e4a	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-05	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125639+00	\N	vitorlopes	\N
da63057d-4f48-4d1c-92ff-9a05c8844d06	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-13	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125727+00	\N	vitorlopes	\N
daa14829-31e4-4754-9682-2b7c6051f2bf	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-09	D	\N	\N	\N	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125684+00	\N	vitorlopes	\N
e09e43fc-d1d0-4659-bc48-dc9d2755e8ca	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-12	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125716+00	\N	vitorlopes	\N
e867b295-67eb-4c82-989d-2a99023bf661	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-04	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125627+00	\N	vitorlopes	\N
e8d5163c-b0b7-4b7f-936a-242a5bf8bce8	cc355bda-2f4a-4044-99c4-2a6447e96d44	2026-09-26	PD	07:00:00	19:00:00	12.00	Regra	3672323a-1f45-480d-b073-9e98fb33f9ca	\N	2026-07-23 12:32:19.125868+00	\N	vitorlopes	\N
e22590e4-3dbc-4dc5-9eb2-8f88334b4a86	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-18	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.201768+00	2026-07-23 19:41:41.204577+00	vitorlopes	vitorlopes
003154a6-6c5e-41e3-ac52-42272f8fc106	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-06	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.01604+00	\N	123	\N
06847927-0ad1-4e17-818c-3076f8b3a67d	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-04	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016481+00	\N	123	\N
070980f6-b0bd-4af4-816a-0b767728bf01	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-07	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016528+00	\N	123	\N
08adc234-3f6a-49c9-9452-4e90057748f2	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-29	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016405+00	\N	123	\N
08e61ea5-b0dd-4be6-a6d5-849aa810bf2c	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-07	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016057+00	\N	123	\N
14acb538-6da9-43b5-9712-cf38cbe7da31	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-20	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016804+00	\N	123	\N
17a8f704-066f-4527-bafb-2791c8f66050	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-22	TL6	\N	\N	6.00	Manual	\N	\N	2026-07-23 23:47:45.016295+00	\N	123	\N
264fa453-c013-4274-8d84-8c3e294b1a70	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-17	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016227+00	\N	123	\N
268307d0-6934-4a5f-851e-59dc22aa95c0	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-05	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.01649+00	\N	123	\N
2b324178-b171-46a7-9a89-91c965bbeb6b	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-26	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.01687+00	\N	123	\N
38c8e5a4-c387-4d63-843e-0ecd56e80e3a	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-02	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.015904+00	\N	123	\N
3b5f2b31-d169-4ce4-960a-e2dfa83a6045	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-14	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016184+00	\N	123	\N
3e0e2150-9c36-4084-b84e-1bde85736ccb	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-01	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016447+00	\N	123	\N
47940ed2-b20d-4b75-817d-dc9c1d453efb	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-14	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016648+00	\N	123	\N
48f393ba-7ef0-40d3-b3a2-0139ed0c2fb8	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-16	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016216+00	\N	123	\N
4cc64a7d-8122-48b5-b77b-b8bb2c244940	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-18	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016247+00	\N	123	\N
5a6ef472-1795-466e-aed0-83ba10d4f480	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-23	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016835+00	\N	123	\N
5f9c0ed2-e92e-4242-9759-3879eddc8068	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-25	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.01686+00	\N	123	\N
69f25c36-a133-4005-a201-aeab5b362cf5	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-11	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016591+00	\N	123	\N
6d719b60-7065-4173-a9d5-a7a9dd5cd7c2	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-03	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.015927+00	\N	123	\N
7004b43e-d7d0-4a87-9786-65476051b6fb	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-13	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016635+00	\N	123	\N
7075dd2a-0b3f-4753-9f21-10f99df8dd5c	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-21	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016286+00	\N	123	\N
78453d77-9be9-490b-a0f4-277b09bbcb21	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-13	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016171+00	\N	123	\N
7f1735ff-c80e-48b9-9e9b-34022b3a3226	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-01	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.015781+00	\N	123	\N
7f8984cc-bf9c-4873-be74-1f84f1526c4c	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-27	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016879+00	\N	123	\N
827b5778-d774-4da5-823c-683f0c894969	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-29	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016904+00	\N	123	\N
8a1faa65-a9c6-409f-8d4f-a9d36ba1a66a	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-26	TL6	\N	\N	6.00	Manual	\N	\N	2026-07-23 23:47:45.01635+00	\N	123	\N
91dccf0e-7f79-4176-a865-3998c7b785a3	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-28	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016888+00	\N	123	\N
95dad89c-d607-481e-9414-e0ad43a76ad7	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-25	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016332+00	\N	123	\N
96892a0c-1f60-4957-b72b-3277557d049b	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-19	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016769+00	\N	123	\N
96b203f2-4f9d-4ce1-96d7-01f5d49c5e75	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-13	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.242941+00	2026-07-23 19:41:42.920333+00	vitorlopes	vitorlopes
99b01bf4-e4a2-494e-9eae-545aa6b2049d	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-05	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016025+00	\N	123	\N
9b7881f2-d271-4849-b1be-f55fef61b53a	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-11	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016127+00	\N	123	\N
9c286cce-c9dd-4634-95d4-2f2d73ec4f0c	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-08	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.01655+00	\N	123	\N
9fa649c0-0129-4ae5-98c3-8ffe374f86bb	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-15	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016195+00	\N	123	\N
a353088d-8909-41a1-867d-abc231b7a304	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-20	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016277+00	\N	123	\N
a3595b4b-fa22-4501-aadf-4ac0d87754be	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-15	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016661+00	\N	123	\N
a4ddb96b-2a59-49cb-a65d-be9dc7af6623	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-22	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016825+00	\N	123	\N
b21ee83e-3629-4a6f-9742-9e7e03f9ee36	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-24	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016322+00	\N	123	\N
b248316c-368d-4eb8-b896-c9c60462e834	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-30	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016421+00	\N	123	\N
b7d3821c-4150-42fe-9340-fd5368fe0c06	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-09	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016105+00	\N	123	\N
b816f2fb-632c-4d1f-bbdf-0bd1b3856dcb	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-17	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016748+00	\N	123	\N
bd60b950-1c9d-4f4a-a7bd-bc9e54ae152e	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-12	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016157+00	\N	123	\N
bdb3a30d-c3d4-4de8-9e62-f40ddb79dd17	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-31	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016924+00	\N	123	\N
bdba4e19-dcc4-466d-9166-991fbf8b40f0	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-16	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016728+00	\N	123	\N
becf42da-8f38-4187-9132-f62e4cbe5d95	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-09	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016566+00	\N	123	\N
c3641571-ae34-4445-8d6e-082d63e74db2	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-21	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016816+00	\N	123	\N
c49d9acc-69ca-4b53-beca-046f148dc1c6	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-03	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016472+00	\N	123	\N
c5a4e027-79cc-42f4-9db6-198ad0f7e699	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-23	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016305+00	\N	123	\N
c6e93728-61ba-4dad-b6bc-dad92ae8900e	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-18	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016759+00	\N	123	\N
c8af88e9-628d-47b0-bfe7-cf7a9f1a75b3	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-02	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016462+00	\N	123	\N
c93d640c-49c0-43ab-ab34-58ad0e530432	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-12	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016619+00	\N	123	\N
d005cff4-59a9-40e9-af1d-e5b52d43dd67	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-10	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-23 23:47:45.016116+00	\N	123	\N
e02533c2-fa36-4574-be53-95e602b1c333	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-06	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016516+00	\N	123	\N
e2ec1964-f672-44b6-8949-d261773c4e70	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-28	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016394+00	\N	123	\N
e577056b-a605-4ec4-8322-99c8c1b69d2b	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-19	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016266+00	\N	123	\N
efc60150-0ea6-4cf8-87ae-2d3f4990dbb2	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-10	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016579+00	\N	123	\N
efec4848-1ade-4a34-9aba-27b2ad9c268b	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-08	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016087+00	\N	123	\N
f369b2bb-4829-4799-92ca-5ed9ab6801ff	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-31	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016437+00	\N	123	\N
fd7ae03a-4828-4dda-9b0a-1e76f3a16a9a	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-30	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016914+00	\N	123	\N
fe88f089-d9e8-4a73-a1ba-c3f61013be53	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-27	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016362+00	\N	123	\N
fedb9fcd-45f1-4048-b501-028e708e6a07	71dfdc78-93aa-48a3-beef-d809cce85334	2026-08-04	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.015991+00	\N	123	\N
ffc44aaa-50e0-44c1-8d81-e5e892a80842	4e95ec87-4478-4d4c-84c1-fc5daa459382	2026-08-24	D	\N	\N	\N	Manual	\N	\N	2026-07-23 23:47:45.016843+00	\N	123	\N
709bcff7-1986-4d39-ac81-067fcc336651	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-10	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869689+00	\N	vitorlopes	\N
78222e62-8532-4cc3-8cd6-a1b4cec47ffc	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-12	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869756+00	\N	vitorlopes	\N
7b762c54-23a9-4e21-9702-cafccb3d3610	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-02	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869415+00	\N	vitorlopes	\N
7cf7f6de-97f7-49ef-8692-844d966be04e	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-06	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.86842+00	\N	vitorlopes	\N
7e5f50df-28ee-4965-8fe8-107adb07105b	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-22	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.868987+00	\N	vitorlopes	\N
7e9a9169-5f43-4378-8518-49352876eb11	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-28	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.870292+00	\N	vitorlopes	\N
84155730-1740-4714-8801-51f3210c21e5	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-16	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868778+00	\N	vitorlopes	\N
8442415f-00f1-4acf-98ad-400ecde04fbf	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-24	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869071+00	\N	vitorlopes	\N
89586f7b-2781-427a-9862-d4a7ff6260a6	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-16	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869886+00	\N	vitorlopes	\N
8fe554a8-654b-40e4-bd1b-dfb34a486123	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-01	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.839743+00	\N	vitorlopes	\N
986be085-4c1a-495d-a9d6-17bc40c2ef39	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-21	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868951+00	\N	vitorlopes	\N
a664c242-a8f5-42ec-b1f4-04beb890dcb3	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-21	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.87005+00	\N	vitorlopes	\N
aae95f3b-987f-419e-8536-8b31f6dfaad6	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-31	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869348+00	\N	vitorlopes	\N
ac9657c9-39c0-4a3f-9913-08573eb10034	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-07	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868456+00	\N	vitorlopes	\N
aca3b439-a1e6-4f44-a88f-8cfcdac4c226	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-03	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.869447+00	\N	vitorlopes	\N
ae223064-c449-499e-a911-3688e20b4226	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-09	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868532+00	\N	vitorlopes	\N
af15b07c-9545-475c-b4c2-080505ba8e8b	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-05	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868384+00	\N	vitorlopes	\N
b05a5e5b-2d42-41dd-b34e-49a2bfc8cf4f	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-04	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869492+00	\N	vitorlopes	\N
b2f0a0a1-3ef1-47e4-aeed-bd2289cb8c7f	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-18	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.868845+00	\N	vitorlopes	\N
b4d0c975-bbe2-4d9b-8b9b-6909c1da1ef9	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-20	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.870012+00	\N	vitorlopes	\N
bba0c30d-c16f-4c4a-873e-d0c2f698cde5	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-11	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.869724+00	\N	vitorlopes	\N
bdbd13dc-0277-4dfd-a399-37d1d623c71b	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-27	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.87026+00	\N	vitorlopes	\N
bf7f1d49-ac31-46e1-aa84-6d87fd3100aa	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-19	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868882+00	\N	vitorlopes	\N
c0b341cc-41c4-48b1-841f-1d40944c09ef	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-07	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.869592+00	\N	vitorlopes	\N
c3415cd8-61b7-48a9-ae88-a03bbccd4bb6	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-14	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.86871+00	\N	vitorlopes	\N
c5c81722-ddf9-47cc-90fb-a2d4facb11bc	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-09	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869657+00	\N	vitorlopes	\N
c886be3f-6b2c-4f61-9d20-39e49e667f14	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-25	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.870198+00	\N	vitorlopes	\N
ca3f628a-fe52-4ca0-a376-2b2cd496ef82	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-22	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.870081+00	\N	vitorlopes	\N
cd0564fb-b388-4f3f-932c-f7f5935f68b7	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-25	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869104+00	\N	vitorlopes	\N
ce50ce37-16f5-444a-9a97-100d51bd1c88	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-23	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.87011+00	\N	vitorlopes	\N
d1821b89-4774-4eaf-a9c4-948b83e72366	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-27	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869205+00	\N	vitorlopes	\N
d3b06f04-f747-41c2-97e1-dafc5ce6d95c	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-23	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869025+00	\N	vitorlopes	\N
dbcda84e-13cd-403a-9a0c-e1a8374cd37b	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-14	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869816+00	\N	vitorlopes	\N
dd642a34-e9d2-46bb-a62e-92f3b051daaa	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-11	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868606+00	\N	vitorlopes	\N
e6b9a0ec-7626-4008-ae80-0d4c0922812f	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-06	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869561+00	\N	vitorlopes	\N
f2535d70-fc27-4202-aefc-a8d056563b31	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-01	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869382+00	\N	vitorlopes	\N
f8ee3ad7-c357-4ffa-91ba-74e6d6d0f0fe	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-29	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869277+00	\N	vitorlopes	\N
dd4c6db8-1d4d-47ae-9bf1-ce8b06eae2ca	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-04	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.20124+00	2026-07-23 19:41:40.249054+00	vitorlopes	vitorlopes
e8493c18-507f-4329-9aef-3eb5f69dcc2a	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-06	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.20132+00	2026-07-23 19:41:40.384555+00	vitorlopes	vitorlopes
61d0a89a-84fc-418f-a619-948e4a1a2372	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-08	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.201394+00	2026-07-23 19:41:40.520312+00	vitorlopes	vitorlopes
5217f031-4370-4734-968f-6506f2855504	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-10	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.201471+00	2026-07-23 19:41:40.655627+00	vitorlopes	vitorlopes
e9bd68a3-2d78-47de-b1e4-8f58ca872808	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-12	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.201548+00	2026-07-23 19:41:40.791929+00	vitorlopes	vitorlopes
a4aa6d8b-ac87-4ee5-95a3-1917bf1fd6ae	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-15	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201657+00	2026-07-23 19:41:40.995754+00	vitorlopes	vitorlopes
4f681f45-43c2-4ac4-a681-4be8c2b24e21	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-16	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.201695+00	2026-07-23 19:41:41.063823+00	vitorlopes	vitorlopes
4b98c2a2-dc8c-40d6-adfd-84da465f8b43	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-19	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201808+00	2026-07-23 19:41:41.272207+00	vitorlopes	vitorlopes
74296879-df91-48d9-84f6-d8e183978bdd	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-20	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.201851+00	2026-07-23 19:41:41.340014+00	vitorlopes	vitorlopes
fb61ea86-63e7-40ea-a377-389f1de290ef	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-22	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.201938+00	2026-07-23 19:41:41.475792+00	vitorlopes	vitorlopes
1bdee06e-42f9-485e-92c8-2b587eed6243	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-24	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.202007+00	2026-07-23 19:41:41.611859+00	vitorlopes	vitorlopes
91b06ce7-2acd-4060-803f-0657add6c3ca	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-26	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.202077+00	2026-07-23 19:41:41.747722+00	vitorlopes	vitorlopes
c56dcd95-8162-4058-95a8-83688ddbba1c	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-28	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.202191+00	2026-07-23 19:41:41.883993+00	vitorlopes	vitorlopes
18dd2781-a5a5-467e-a5cf-64f7a55328a7	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-30	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.202288+00	2026-07-23 19:41:42.020425+00	vitorlopes	vitorlopes
800b0224-def1-4a0c-b78b-fdf44fef2966	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-03	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.242508+00	2026-07-23 19:41:42.233337+00	vitorlopes	vitorlopes
80b49e14-5483-4da5-b648-72ca961396e7	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-05	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.242584+00	2026-07-23 19:41:42.371374+00	vitorlopes	vitorlopes
8adb7542-7fd6-4351-9862-68304098fc28	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-07	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.242656+00	2026-07-23 19:41:42.508018+00	vitorlopes	vitorlopes
5d9023d4-1469-43d1-9ede-0a4aa607f7ce	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-10	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.242832+00	2026-07-23 19:41:42.71582+00	vitorlopes	vitorlopes
00d0d74b-37d9-4cda-8cdb-1915d5236c7f	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-28	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869245+00	\N	vitorlopes	\N
fe0a9c71-f61c-43c2-acf0-777b635480f4	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-11	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.242871+00	2026-07-23 19:41:42.784474+00	vitorlopes	vitorlopes
40d1c186-bbff-4281-96c0-54efbcbd967e	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-14	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.242976+00	2026-07-23 19:41:42.987879+00	vitorlopes	vitorlopes
725e940f-e5a3-44cc-91a0-dd379c4d6d38	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-15	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.243011+00	2026-07-23 19:41:43.055649+00	vitorlopes	vitorlopes
81a738c7-8f19-4da9-9704-db7f79223ca8	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-17	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.243081+00	2026-07-23 19:41:43.195795+00	vitorlopes	vitorlopes
17755296-124a-4dd7-96d0-cc06d5056100	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-19	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.243152+00	2026-07-23 19:41:43.331672+00	vitorlopes	vitorlopes
0637c87a-f108-477e-a853-3c3c51d8973d	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-22	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.243255+00	2026-07-23 19:41:43.536585+00	vitorlopes	vitorlopes
33636284-a822-403f-968c-1e42d5aff1e0	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-23	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.243291+00	2026-07-23 19:41:43.604143+00	vitorlopes	vitorlopes
cf2962e6-c4f9-44aa-a7a4-16fcf17856a2	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-25	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.243359+00	2026-07-23 19:41:43.743764+00	vitorlopes	vitorlopes
f2961907-d33f-4aae-8242-31047ceb3a85	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-27	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.243429+00	2026-07-23 19:41:43.880057+00	vitorlopes	vitorlopes
2cc55e94-381d-41cb-ad42-cb42b0027dea	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-29	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.243499+00	2026-07-23 19:41:44.016007+00	vitorlopes	vitorlopes
256ee953-0d69-4df1-94a1-aa63d748fcde	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-07	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.26563+00	2026-07-23 19:41:44.559663+00	vitorlopes	vitorlopes
01be7831-862b-4ee4-bdce-03b9f797a2dc	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-18	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869946+00	\N	vitorlopes	\N
b62ac058-53f6-48a7-8284-95743e650d59	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-02	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.200882+00	2026-07-23 19:41:40.111638+00	vitorlopes	vitorlopes
a346e64f-6dfd-46b3-85da-49e164ad98ee	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-03	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201196+00	2026-07-23 19:41:40.180284+00	vitorlopes	vitorlopes
5031b008-1a0d-499b-9d9a-5c030e86ee18	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-05	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201277+00	2026-07-23 19:41:40.315928+00	vitorlopes	vitorlopes
5373a2df-f5a8-48bc-92ab-95de7000c51b	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-07	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201358+00	2026-07-23 19:41:40.45226+00	vitorlopes	vitorlopes
d287c17f-2b7e-42dd-a8eb-078736187da7	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-09	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201433+00	2026-07-23 19:41:40.587924+00	vitorlopes	vitorlopes
2937b44d-e6f5-425f-8bdc-a6889bac61b8	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-11	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201512+00	2026-07-23 19:41:40.723897+00	vitorlopes	vitorlopes
64c1c7ac-c6a0-4b3d-8e21-9ba530150c80	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-13	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201585+00	2026-07-23 19:41:40.859625+00	vitorlopes	vitorlopes
1150d94c-543d-4220-83b5-025c9ca84501	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-17	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.20173+00	2026-07-23 19:41:41.136723+00	vitorlopes	vitorlopes
108eb9f4-f382-471a-b09c-fa4098e06991	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-21	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201903+00	2026-07-23 19:41:41.407885+00	vitorlopes	vitorlopes
9db3db17-6648-409f-9231-97bae13b52ae	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-23	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.201973+00	2026-07-23 19:41:41.543682+00	vitorlopes	vitorlopes
28283ed1-a8cf-41b6-b0fc-5fc53d0d1c7e	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-25	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.202043+00	2026-07-23 19:41:41.680232+00	vitorlopes	vitorlopes
6c5084e8-3a19-4309-b242-089da36b5612	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-27	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.202135+00	2026-07-23 19:41:41.81604+00	vitorlopes	vitorlopes
339accd4-6c24-4028-a660-6d88703e39ab	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-29	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.202232+00	2026-07-23 19:41:41.952505+00	vitorlopes	vitorlopes
67981895-5e93-4199-a21b-46ff593a2f64	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-01	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.242309+00	2026-07-23 19:41:42.091978+00	vitorlopes	vitorlopes
cd6ea4fc-aa0e-4e46-b470-238b6fc85a00	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-02	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.242454+00	2026-07-23 19:41:42.160656+00	vitorlopes	vitorlopes
f6c40c3b-1b0f-4474-a9f8-2740a59ecb4d	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-04	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.242547+00	2026-07-23 19:41:42.304271+00	vitorlopes	vitorlopes
66839f92-927e-4031-807f-aab2e5b9ebaf	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-06	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.242619+00	2026-07-23 19:41:42.439711+00	vitorlopes	vitorlopes
620f31b4-42ae-4471-911d-f8ddbd032a5c	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-08	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.242724+00	2026-07-23 19:41:42.57643+00	vitorlopes	vitorlopes
331ebf55-4ec0-4f3b-ac75-119ccf59f665	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-12	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.242907+00	2026-07-23 19:41:42.851956+00	vitorlopes	vitorlopes
e1f87d50-e4c5-4a8a-80e0-ff0f248e6dad	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-16	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.243046+00	2026-07-23 19:41:43.123399+00	vitorlopes	vitorlopes
14078f19-9563-4215-897a-ab86682924bd	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-18	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.243116+00	2026-07-23 19:41:43.263702+00	vitorlopes	vitorlopes
1c378d68-9af7-4d1e-9af7-3908f21848af	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-20	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.243187+00	2026-07-23 19:41:43.399877+00	vitorlopes	vitorlopes
25e0a279-ab02-498e-8225-375cf12ab1b9	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-24	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.243325+00	2026-07-23 19:41:43.671892+00	vitorlopes	vitorlopes
ceb69b42-06f2-4eb3-9e9c-bd34672abe27	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-26	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.243394+00	2026-07-23 19:41:43.81189+00	vitorlopes	vitorlopes
d54a6f02-8901-4e2a-840f-9f4c21034494	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-28	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.243464+00	2026-07-23 19:41:43.947735+00	vitorlopes	vitorlopes
0cda9b21-bf99-4f80-9377-0c5aa3e8e1e0	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-30	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.243533+00	2026-07-23 19:41:44.085244+00	vitorlopes	vitorlopes
20685ea2-0f75-42b4-8ccd-931d4216b24a	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-18	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.26601+00	2026-07-23 19:41:45.307711+00	vitorlopes	vitorlopes
37747b1c-17aa-4700-b38c-c88c9c5ca841	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-15	LM	07:00:00	19:00:00	\N	Manual	\N	\N	2026-07-23 17:02:01.265907+00	2026-07-23 19:41:45.104511+00	vitorlopes	vitorlopes
e00f0ab0-0074-434b-a2a2-deb4360e5e63	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-16	LM	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.265941+00	2026-07-23 19:41:45.171778+00	vitorlopes	vitorlopes
190b9ba7-0962-4f6e-b701-28230d4ae663	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-29	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.870323+00	\N	vitorlopes	\N
8f81e5a8-29c0-4bc8-8fd1-a222c10d2598	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-17	LM	07:00:00	19:00:00	\N	Manual	\N	\N	2026-07-23 17:02:01.265974+00	2026-07-23 19:41:45.239656+00	vitorlopes	vitorlopes
27674a53-52fb-4aca-911b-a704565251cb	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-01	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.200141+00	2026-07-23 19:41:40.043685+00	vitorlopes	vitorlopes
1c11c4c0-eab5-403c-8c39-663e651bd369	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-13	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868673+00	\N	vitorlopes	\N
1c7446f9-ad76-4a5c-bfae-d2c2ef63b73f	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-10	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.868567+00	\N	vitorlopes	\N
2147bb73-c98f-45d7-85d9-04e7b9c6c37c	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-19	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.869978+00	\N	vitorlopes	\N
22d1cb25-77de-4b74-9a40-8f0b1587f763	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-02	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.866783+00	\N	vitorlopes	\N
231fd31e-ecc6-4ee4-bfca-f81ff70813bd	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-05	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869529+00	\N	vitorlopes	\N
25aebd71-5fbc-4229-821c-37f98c9c55b1	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-08	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869627+00	\N	vitorlopes	\N
e636ca78-22ac-4211-88e8-d0bfd1edd842	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-19	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.266044+00	2026-07-23 19:41:45.375563+00	vitorlopes	vitorlopes
67dc2f9f-cb74-412f-9ac2-cc346a31a8ad	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-20	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.266079+00	2026-07-23 19:41:45.443692+00	vitorlopes	vitorlopes
aabd12f5-3256-4786-a1f2-88f0e93ed42a	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-21	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.266142+00	2026-07-23 19:41:45.51154+00	vitorlopes	vitorlopes
8fd048d2-b4ed-40fb-9aee-8e7b31b01b4c	61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	2026-11-14	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.20162+00	2026-07-23 19:41:40.928294+00	vitorlopes	vitorlopes
0e954446-45ad-40e3-80ff-886e1b1c28e7	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-09	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.242785+00	2026-07-23 19:41:42.647826+00	vitorlopes	vitorlopes
0dbeb234-7655-4214-aa5e-f74cdbfc12d9	5c209eaf-0043-41f8-9324-d8edbee5704e	2026-11-21	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.243221+00	2026-07-23 19:41:43.46758+00	vitorlopes	vitorlopes
a04e60f7-bf09-47e6-92ea-86211dc62084	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-01	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.265342+00	2026-07-23 19:41:44.152651+00	vitorlopes	vitorlopes
2cd4e849-92f3-42c8-8c16-2f50efb14242	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-03	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868288+00	\N	vitorlopes	\N
3a974c31-c980-443c-a20c-b6a188f8189c	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-04	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868341+00	\N	vitorlopes	\N
3c4379fc-8b56-4034-8c9d-16e0f3a71a51	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-26	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.87023+00	\N	vitorlopes	\N
40304085-480c-4227-820e-ed526fcb0a60	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-08	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868494+00	\N	vitorlopes	\N
42985474-edb8-41fe-adc6-c74a420febe3	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-17	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869915+00	\N	vitorlopes	\N
4542fff5-a949-4142-8050-edfd7b3a148f	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-17	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868809+00	\N	vitorlopes	\N
4dd1fa70-9d5f-4510-a32f-f6ad0ab1f5f4	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-02	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.265442+00	2026-07-23 19:41:44.220089+00	vitorlopes	vitorlopes
ab1610e6-d153-4fcc-845f-72498e699f4c	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-03	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.265483+00	2026-07-23 19:41:44.288281+00	vitorlopes	vitorlopes
54a55ab3-28f1-47dc-a62c-8534a0d0587f	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-04	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.265521+00	2026-07-23 19:41:44.356286+00	vitorlopes	vitorlopes
71a43fed-ffaa-4490-9654-a760fe38ecd2	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-05	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.265557+00	2026-07-23 19:41:44.424053+00	vitorlopes	vitorlopes
deb4b882-79fc-4b53-9efc-c4252d751cfc	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-06	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.265594+00	2026-07-23 19:41:44.491739+00	vitorlopes	vitorlopes
6cb72c86-1ca1-4c0d-a80f-92e43c90c90a	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-22	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.266198+00	2026-07-23 19:41:45.579653+00	vitorlopes	vitorlopes
b5201f7c-0292-4bc8-aeec-5fce8474651a	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-23	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.266233+00	2026-07-23 19:41:45.648035+00	vitorlopes	vitorlopes
49458391-63f8-4f14-938e-97e6cf9a799e	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-31	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.870382+00	\N	vitorlopes	\N
4ab0ccaf-b738-43e1-8f8c-621473779cf7	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-12	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868639+00	\N	vitorlopes	\N
4b1cc6b3-8101-46f5-864d-5358c63f2bcd	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-26	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.869139+00	\N	vitorlopes	\N
512391cf-e97c-4b12-970e-734ba3a7f01a	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-24	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.870152+00	\N	vitorlopes	\N
597fbc95-fa9b-4cbf-a969-a8b686342f39	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-15	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868744+00	\N	vitorlopes	\N
5ef4cbf3-d3cb-4dfa-bb6a-54f0a74e9fc8	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-15	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.869854+00	\N	vitorlopes	\N
ea6f132a-b496-4491-a5fb-0095b67604cb	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-24	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.266268+00	2026-07-23 19:41:45.716374+00	vitorlopes	vitorlopes
33131b2f-61c9-48f3-ab8d-9850314dafe3	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-25	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.266303+00	2026-07-23 19:41:45.784492+00	vitorlopes	vitorlopes
a8808b5e-630c-4228-aaa9-7020a47c74e5	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-26	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.266395+00	2026-07-23 19:41:45.851835+00	vitorlopes	vitorlopes
aff5dcb6-f779-427c-8da1-f0a9eda16ab3	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-27	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.266438+00	2026-07-23 19:41:45.920185+00	vitorlopes	vitorlopes
63d36500-b09b-486b-8c9f-1294450f3194	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-30	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.870353+00	\N	vitorlopes	\N
fdda62d7-c419-4906-b183-be5e16360d59	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-28	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.266475+00	2026-07-23 19:41:45.988011+00	vitorlopes	vitorlopes
67409d67-99d4-4faa-a871-faf876099f93	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-20	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.868916+00	\N	vitorlopes	\N
e9c35fe8-22b9-4e88-bcd0-5e657a5037c6	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-08	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.265664+00	2026-07-23 19:41:44.627536+00	vitorlopes	vitorlopes
4de3fe03-4a28-4c5e-930a-b1104d8f4f18	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-29	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.266512+00	2026-07-23 19:41:46.056201+00	vitorlopes	vitorlopes
5f307b2e-944d-4a24-816b-8501c8a09487	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-09	PD	07:00:00	19:00:00	12.00	Manual	\N	\N	2026-07-23 17:02:01.265699+00	2026-07-23 19:41:44.697051+00	vitorlopes	vitorlopes
960e13f3-2fb0-4fbb-b617-3b47eabec9ee	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-30	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.266549+00	2026-07-23 19:41:46.123704+00	vitorlopes	vitorlopes
8e401781-1e6e-4008-bba5-e882cf79284f	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-10	D	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.265734+00	2026-07-23 19:41:44.763855+00	vitorlopes	vitorlopes
92e0760f-2ea5-4fc7-bef6-10f18428816d	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-11	LM	07:00:00	19:00:00	\N	Manual	\N	\N	2026-07-23 17:02:01.265769+00	2026-07-23 19:41:44.832097+00	vitorlopes	vitorlopes
f6a79949-27dc-4384-9cb6-3cce8af76e0a	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-12	LM	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.265803+00	2026-07-23 19:41:44.89951+00	vitorlopes	vitorlopes
d55a35d6-25ec-4681-9ebb-56e79e1934a8	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-13	LM	07:00:00	19:00:00	\N	Manual	\N	\N	2026-07-23 17:02:01.265837+00	2026-07-23 19:41:44.968045+00	vitorlopes	vitorlopes
816d5b22-207e-4d65-8913-5e517eeaee39	6e90b0a8-949c-49f5-a25d-74eaed2ff21f	2026-11-14	LM	\N	\N	\N	Manual	\N	\N	2026-07-23 17:02:01.265873+00	2026-07-23 19:41:45.035902+00	vitorlopes	vitorlopes
6b885472-ce16-4701-a331-87b5ab7a3e71	5288a194-90a6-48c1-923a-ee7d3451ccd4	2026-10-13	D	\N	\N	\N	Manual	\N	\N	2026-07-24 01:53:18.869786+00	\N	vitorlopes	\N
6c93a1fc-16ea-4b51-a58c-50f5555319cd	d03d5db6-362d-488c-aebc-fc171c8d3a6d	2026-10-30	PT	07:00:00	07:00:00	24.00	Manual	\N	\N	2026-07-24 01:53:18.869312+00	\N	vitorlopes	\N
\.


--
-- Data for Name: EscalaServidor; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."EscalaServidor" ("Id", "EscalaId", "ServidorId", "CargoId", "Ordem", "ServidorNome", "Matricula", "CargoNome", "CargoCodigo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
99328c91-43dc-44e4-94cb-306d64fa7795	26666fe7-1db5-44e9-ba59-f44b2244821c	c3baf373-ff8b-4635-af22-9934234cbd92	174cf381-fb97-4301-aa54-227cbc179b23	1	Ricardo Oliveira	123.412-3	Perito Criminal	PERITO_CRIMINAL	2026-07-22 02:57:19.452242+00	\N	vitorlopes	\N
61f6cf54-d678-474b-8a0d-c042ec64ec82	26666fe7-1db5-44e9-ba59-f44b2244821c	6de7afd2-510c-483a-94db-fa716e74138c	174cf381-fb97-4301-aa54-227cbc179b23	2	Rosângela D'Avilla	220.547-5	Perito Criminal	PERITO_CRIMINAL	2026-07-22 02:57:23.481545+00	\N	vitorlopes	\N
d2f75f91-7926-4d9b-9fe3-0a3e6b1f7f07	26666fe7-1db5-44e9-ba59-f44b2244821c	22222222-2222-2222-2222-222222222222	174cf381-fb97-4301-aa54-227cbc179b23	3	Vitor Lopes	00.000-1	Perito Criminal	PERITO_CRIMINAL	2026-07-22 02:57:26.184159+00	\N	vitorlopes	\N
383d9e62-67d1-4e02-ba26-abb51f432c68	436591f8-6d78-4cdb-9453-e1b17dfab17a	c3baf373-ff8b-4635-af22-9934234cbd92	174cf381-fb97-4301-aa54-227cbc179b23	1	Ricardo Oliveira	123.412-3	Perito Criminal	PERITO_CRIMINAL	2026-07-23 12:32:19.087987+00	\N	vitorlopes	\N
6f343a4b-3041-4b82-a1c7-9b2129f33d93	436591f8-6d78-4cdb-9453-e1b17dfab17a	6de7afd2-510c-483a-94db-fa716e74138c	174cf381-fb97-4301-aa54-227cbc179b23	2	Rosângela D'Avilla	220.547-5	Perito Criminal	PERITO_CRIMINAL	2026-07-23 12:32:19.107389+00	\N	vitorlopes	\N
cc355bda-2f4a-4044-99c4-2a6447e96d44	436591f8-6d78-4cdb-9453-e1b17dfab17a	22222222-2222-2222-2222-222222222222	174cf381-fb97-4301-aa54-227cbc179b23	3	Vitor Lopes	00.000-1	Perito Criminal	PERITO_CRIMINAL	2026-07-23 12:32:19.116647+00	\N	vitorlopes	\N
5288a194-90a6-48c1-923a-ee7d3451ccd4	ac95eee5-f4d3-4ca2-bf0f-221bbf8a6f93	22222222-2222-2222-2222-222222222222	174cf381-fb97-4301-aa54-227cbc179b23	3	Vitor Lopes	00.000-1	Perito Criminal	PERITO_CRIMINAL	2026-07-23 12:35:47.917117+00	\N	vitorlopes	\N
d03d5db6-362d-488c-aebc-fc171c8d3a6d	ac95eee5-f4d3-4ca2-bf0f-221bbf8a6f93	6de7afd2-510c-483a-94db-fa716e74138c	174cf381-fb97-4301-aa54-227cbc179b23	2	Rosângela D'Avilla	220.547-5	Perito Criminal	PERITO_CRIMINAL	2026-07-23 12:35:47.917106+00	\N	vitorlopes	\N
61d7e18b-d8ba-4f15-b337-f787f0b0e5d1	4cfc9da6-8e52-4106-872c-f5f9b84d745a	c3baf373-ff8b-4635-af22-9934234cbd92	174cf381-fb97-4301-aa54-227cbc179b23	1	Ricardo Oliveira	123.412-3	Perito Criminal	PERITO_CRIMINAL	2026-07-23 16:12:02.539813+00	\N	vitorlopes	\N
5c209eaf-0043-41f8-9324-d8edbee5704e	4cfc9da6-8e52-4106-872c-f5f9b84d745a	6de7afd2-510c-483a-94db-fa716e74138c	174cf381-fb97-4301-aa54-227cbc179b23	2	Rosângela D'Avilla	220.547-5	Perito Criminal	PERITO_CRIMINAL	2026-07-23 16:12:02.685011+00	\N	vitorlopes	\N
6e90b0a8-949c-49f5-a25d-74eaed2ff21f	4cfc9da6-8e52-4106-872c-f5f9b84d745a	22222222-2222-2222-2222-222222222222	174cf381-fb97-4301-aa54-227cbc179b23	3	Vitor Lopes	00.000-1	Perito Criminal	PERITO_CRIMINAL	2026-07-23 16:12:02.715485+00	\N	vitorlopes	\N
4e95ec87-4478-4d4c-84c1-fc5daa459382	9f8fc8cc-819c-4c4f-8535-20e84d5385fd	c3baf373-ff8b-4635-af22-9934234cbd92	9b80a4b6-4a6c-43c1-a08c-a30e1814644b	2	Ricardo Oliveira	123.412-3	Agente Técnico Forense	ATF	2026-07-23 22:06:12.071737+00	\N	123	\N
71dfdc78-93aa-48a3-beef-d809cce85334	9f8fc8cc-819c-4c4f-8535-20e84d5385fd	b2536338-d43c-431f-b516-4cd524c3d78f	76470c0b-7c46-456f-8c10-053e75a92358	1	Alyson Costa	123.123-2	Assistente Técnico Forense	ASTF	2026-07-23 22:06:12.054936+00	\N	123	\N
\.


--
-- Data for Name: Nucleo; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Nucleo" ("Id", "Nome", "ChefeServidorId", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "Sigla") FROM stdin;
012c969b-af18-4af8-8d21-ebba48ac59a5	laboratorios nucleo	\N	2026-07-21 22:39:04.980984+00	2026-07-22 00:31:39.365211+00	vitorlopes	vitorlopes	LABORATO
52213705-b435-45f4-a4a7-d7d81a68729e	Custodia	\N	2026-07-21 22:42:11.54006+00	2026-07-22 03:59:26.159629+00	vitorlopes	vitorlopes	CUSTODIA
7f1dfbfe-c74c-46c6-876f-ca44059ec84a	testereraerarewrwrwrewrwwwwwwwwwwwwwwwwwwwwwwwwwwwwwweeeeeeeeeeeeeeeeeeee	\N	2026-07-24 02:02:33.674008+00	2026-07-24 02:03:39.164877+00	vitorlopes	vitorlopes	testete
\.


--
-- Data for Name: PadraoEscala; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PadraoEscala" ("Id", "Codigo", "Nome", "TipoFuncionamento", "TipoJornada", "RecorrenciaTipo", "DiasTrabalho", "DiasFolga", "DiasSemana", "TipoOcorrenciaTrabalho", "TipoOcorrenciaFolga", "HoraInicioPadrao", "HoraFimPadrao", "HorasPadrao", "Sistema", "Ativo", "SetorId", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
01c0f0b1-8738-4063-a445-4775a844a380	PLANTAO_NOTURNO	Plantão Noturno	VinteQuatroHoras	Plantao	CicloPlantao	1	1	\N	PN	D	19:00:00	07:00:00	12.00	t	t	\N	2026-07-22 03:44:24.110522+00	\N	seed	\N
1e025d54-2bda-4be4-9139-ebd0b096251c	12X36	12x36	VinteQuatroHoras	Plantao	CicloPlantao	1	1	\N	PD	D	07:00:00	19:00:00	12.00	t	t	\N	2026-07-22 03:44:24.110412+00	\N	seed	\N
527ac5d5-5fbc-4405-a744-c354b8a7a633	24X48	24x48	VinteQuatroHoras	Plantao	CicloPlantao	1	2	\N	PT	D	07:00:00	07:00:00	24.00	t	t	\N	2026-07-22 03:44:24.110361+00	\N	seed	\N
5cc40f20-e248-4668-8849-0b98c736f22d	24X72	24x72	VinteQuatroHoras	Plantao	CicloPlantao	1	3	\N	PT	D	07:00:00	07:00:00	24.00	t	t	\N	2026-07-22 03:44:24.109188+00	\N	seed	\N
727cbb72-a798-4f54-9af2-f00aa0bd70a1	PLANTAO_DIURNO	Plantão Diurno	VinteQuatroHoras	Plantao	CicloPlantao	1	1	\N	PD	D	07:00:00	19:00:00	12.00	t	t	\N	2026-07-22 03:44:24.110475+00	\N	seed	\N
ac1f5e92-f22d-4251-abd6-e84b364c02e4	PERSONALIZADO	Personalizado	VinteQuatroHoras	Outro	CicloPlantao	1	3	\N	PT	D	\N	\N	\N	t	t	\N	2026-07-22 03:44:24.110548+00	\N	seed	\N
ed5caa85-39fe-45a9-a35b-9fab3cfa5121	EXP_ADM	Expediente Administrativo	Expediente	Expediente	DiasSemana	\N	\N	1,2,3,4,5	M	D	08:00:00	14:00:00	6.00	t	t	\N	2026-07-22 03:44:24.071303+00	\N	seed	\N
\.


--
-- Data for Name: Perfil; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Perfil" ("Id", "Nome", "Codigo", "Descricao", "Sistema", "Ativo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	Super Administrador	SUPERADMINISTRADOR	Acesso total à plataforma	t	t	2026-07-21 01:13:10.306146+00	\N	seed	\N
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	Chefe de Setor	CHEFE_SETOR	Gestão operacional do setor	t	t	2026-07-21 01:13:10.337037+00	2026-07-23 20:25:21.113369+00	seed	vitorlopes
cccccccc-cccc-cccc-cccc-cccccccccccc	Servidor	SERVIDOR	Acesso básico do servidor	t	t	2026-07-21 01:13:10.339684+00	2026-07-24 00:38:06.137651+00	seed	vitorlopes
\.


--
-- Data for Name: PerfilPermissao; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PerfilPermissao" ("PerfilId", "PermissaoId") FROM stdin;
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	19b2a7c2-088f-6f42-83d6-dc2df12f7785
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	350039ae-f8ad-7243-b5e6-cf618d30214e
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	397115eb-4e17-1b41-92b7-6ca39cf58ada
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	42019a35-967e-8b45-9a27-4280f1e4e1ff
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	47488808-3939-9345-b668-8ac46b0061eb
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	4c94c3ae-3bcd-4447-8435-9a1a5f7581dd
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	5685c66f-666b-0344-8671-34035e2aa378
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	7245e7f0-61f6-0c45-be8d-cca2073b6070
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	7420a328-9918-d240-a575-d2a4259f2eb5
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	89195584-2a56-0740-aeea-42c73c2d79d8
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	9c6c5628-69e7-b744-bf2c-7cd0439bc9f4
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	a59dd1f5-f7ac-5c40-8b3c-64e7c942a45c
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	aad0bb28-1c4b-b542-98c0-7eeb5b9d454c
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	b101efd3-9c98-8c4d-aaa0-85585c3b3cd7
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	bd74f1d4-ae3e-9e45-a6f4-92a1d7a52393
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	f583299f-145e-eb46-bc56-ed35ee4fd798
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	230ddc31-76e6-e140-a610-cb7dff6caa47
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	3f360850-0a1f-464a-9d57-47af589ac750
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	74da3136-007e-d44d-9bb5-c416cbde5517
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	a0177a9e-9a12-b241-9358-78a1deb15fb7
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	28eb1d40-b237-7341-ab2d-9699cfaf0b41
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	6694f852-8b40-0d46-91b1-1cea9e8103fb
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	88b1b5df-41e1-c543-bc12-7cabb962f627
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	8e1400bc-489e-aa43-8070-36a4f8cb73f9
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	9049e2d5-ae74-5a45-96b4-e018f52a93d0
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	94f93690-dcd9-0f49-ac7d-a2b8f83b5fbe
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	b83b7094-a472-2b4d-b493-f30bd299b6e7
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	88b1b5df-41e1-c543-bc12-7cabb962f627
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	8e1400bc-489e-aa43-8070-36a4f8cb73f9
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	9049e2d5-ae74-5a45-96b4-e018f52a93d0
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	94f93690-dcd9-0f49-ac7d-a2b8f83b5fbe
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	b83b7094-a472-2b4d-b493-f30bd299b6e7
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	0859dc8e-8b11-9b45-8477-7e7e4cb3a806
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	93ee4437-ec79-3c4c-b7ee-6b2395977320
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	a1975da1-8acb-b547-b460-c771e9728617
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	d2c43544-1ee6-004c-be01-095e928922a9
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	0859dc8e-8b11-9b45-8477-7e7e4cb3a806
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	93ee4437-ec79-3c4c-b7ee-6b2395977320
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	a1975da1-8acb-b547-b460-c771e9728617
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	80e1bedf-f79a-3347-b801-ea5da24bdeca
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	c14d090b-2236-914b-b9ea-407bc2e4453d
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	c43359d1-fd50-6048-b7ca-43b7da281f90
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	f7cab90b-b061-964e-adae-6e1f5f7a63c7
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	80e1bedf-f79a-3347-b801-ea5da24bdeca
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	c14d090b-2236-914b-b9ea-407bc2e4453d
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	c43359d1-fd50-6048-b7ca-43b7da281f90
bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb	f7cab90b-b061-964e-adae-6e1f5f7a63c7
cccccccc-cccc-cccc-cccc-cccccccccccc	230ddc31-76e6-e140-a610-cb7dff6caa47
cccccccc-cccc-cccc-cccc-cccccccccccc	4c94c3ae-3bcd-4447-8435-9a1a5f7581dd
cccccccc-cccc-cccc-cccc-cccccccccccc	b101efd3-9c98-8c4d-aaa0-85585c3b3cd7
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa	b67e8e61-45f9-7646-b2f8-6ad79b590018
\.


--
-- Data for Name: Permissao; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Permissao" ("Id", "Codigo", "Nome", "Descricao", "Modulo", "Sistema", "Ativo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "Area") FROM stdin;
19b2a7c2-088f-6f42-83d6-dc2df12f7785	perfis.listar	Listar perfis	Visualizar perfis de acesso	perfis	t	t	2026-07-21 01:13:10.240309+00	\N	seed	\N	Administração do Sistema
2b3c2c42-0647-7240-b263-15139f06f19b	permissoes.excluir	Excluir permissões	Desativar permissões	permissoes	t	t	2026-07-21 01:13:10.240497+00	\N	seed	\N	Administração do Sistema
350039ae-f8ad-7243-b5e6-cf618d30214e	perfis.editar	Editar perfis	Alterar dados de perfis	perfis	t	t	2026-07-21 01:13:10.240378+00	\N	seed	\N	Administração do Sistema
397115eb-4e17-1b41-92b7-6ca39cf58ada	perfis.excluir	Excluir perfis	Desativar ou remover perfis	perfis	t	t	2026-07-21 01:13:10.240398+00	\N	seed	\N	Administração do Sistema
42019a35-967e-8b45-9a27-4280f1e4e1ff	setores.criar	Criar setores	Cadastrar setores	setores	t	t	2026-07-21 01:13:10.240523+00	\N	seed	\N	Gestão Institucional
47488808-3939-9345-b668-8ac46b0061eb	servidores.editar	Editar servidores	Alterar servidores	servidores	t	t	2026-07-21 01:13:10.240591+00	\N	seed	\N	Gestão Institucional
48f58cf8-ae76-624c-83ab-c1229e778c73	permissoes.editar	Editar permissões	Alterar permissões	permissoes	t	t	2026-07-21 01:13:10.240476+00	\N	seed	\N	Administração do Sistema
4c94c3ae-3bcd-4447-8435-9a1a5f7581dd	servidores.listar	Listar servidores	Visualizar servidores	servidores	t	t	2026-07-21 01:13:10.240558+00	\N	seed	\N	Gestão Institucional
5685c66f-666b-0344-8671-34035e2aa378	usuarios.listar	Listar usuários	Visualizar usuários do sistema	usuarios	t	t	2026-07-21 01:13:10.181521+00	\N	seed	\N	Administração do Sistema
7245e7f0-61f6-0c45-be8d-cca2073b6070	servidores.criar	Criar servidores	Cadastrar servidores	servidores	t	t	2026-07-21 01:13:10.24057+00	\N	seed	\N	Gestão Institucional
7420a328-9918-d240-a575-d2a4259f2eb5	perfis.criar	Criar perfis	Cadastrar novos perfis	perfis	t	t	2026-07-21 01:13:10.240341+00	\N	seed	\N	Administração do Sistema
7699093b-5ce9-fd4f-92ad-ebcd8eaa390d	permissoes.criar	Criar permissões	Cadastrar novas permissões	permissoes	t	t	2026-07-21 01:13:10.240459+00	\N	seed	\N	Administração do Sistema
9c6c5628-69e7-b744-bf2c-7cd0439bc9f4	usuarios.bloquear	Bloquear usuários	Bloquear ou desbloquear acesso	usuarios	t	t	2026-07-21 01:13:10.240245+00	\N	seed	\N	Administração do Sistema
a59dd1f5-f7ac-5c40-8b3c-64e7c942a45c	setores.editar	Editar setores	Alterar setores	setores	t	t	2026-07-21 01:13:10.240545+00	\N	seed	\N	Gestão Institucional
aad0bb28-1c4b-b542-98c0-7eeb5b9d454c	perfis.gerenciar_permissoes	Gerenciar permissões do perfil	Associar ou remover permissões de um perfil	perfis	t	t	2026-07-21 01:13:10.240418+00	\N	seed	\N	Administração do Sistema
b101efd3-9c98-8c4d-aaa0-85585c3b3cd7	setores.listar	Listar setores	Visualizar setores	setores	t	t	2026-07-21 01:13:10.240511+00	\N	seed	\N	Gestão Institucional
bd74f1d4-ae3e-9e45-a6f4-92a1d7a52393	usuarios.editar	Editar usuários	Alterar dados e perfis de usuários	usuarios	t	t	2026-07-21 01:13:10.240203+00	\N	seed	\N	Administração do Sistema
f583299f-145e-eb46-bc56-ed35ee4fd798	usuarios.criar	Criar usuários	Cadastrar novos usuários	usuarios	t	t	2026-07-21 01:13:10.239507+00	\N	seed	\N	Administração do Sistema
230ddc31-76e6-e140-a610-cb7dff6caa47	cargos.listar	Listar cargos	Visualizar cargos oficiais	cargos	t	t	2026-07-21 22:33:07.581617+00	\N	seed	\N	Gestão Institucional
3f360850-0a1f-464a-9d57-47af589ac750	nucleos.listar	Listar núcleos	Visualizar núcleos da estrutura organizacional	nucleos	t	t	2026-07-21 22:33:07.528099+00	\N	seed	\N	Gestão Institucional
74da3136-007e-d44d-9bb5-c416cbde5517	nucleos.editar	Editar núcleos	Alterar núcleos	nucleos	t	t	2026-07-21 22:33:07.581587+00	\N	seed	\N	Gestão Institucional
a0177a9e-9a12-b241-9358-78a1deb15fb7	nucleos.criar	Criar núcleos	Cadastrar núcleos	nucleos	t	t	2026-07-21 22:33:07.581001+00	\N	seed	\N	Gestão Institucional
89195584-2a56-0740-aeea-42c73c2d79d8	permissoes.listar	Listar permissões	Visualizar catálogo de permissões do sistema	permissoes	t	t	2026-07-21 01:13:10.240442+00	2026-07-23 02:34:06.469768+00	seed	seed	Administração do Sistema
28eb1d40-b237-7341-ab2d-9699cfaf0b41	setores.excluir	Excluir setores	Remover setores sem servidores lotados	setores	t	t	2026-07-22 01:39:45.557831+00	\N	seed	\N	Gestão Institucional
6694f852-8b40-0d46-91b1-1cea9e8103fb	nucleos.excluir	Excluir núcleos	Remover núcleos sem setores vinculados	nucleos	t	t	2026-07-22 01:39:45.496676+00	\N	seed	\N	Gestão Institucional
88b1b5df-41e1-c543-bc12-7cabb962f627	escalas.criar	Criar escalas	Cadastrar e copiar escalas	escalas	t	t	2026-07-22 01:39:45.558386+00	\N	seed	\N	Gestão do Setor
9049e2d5-ae74-5a45-96b4-e018f52a93d0	escalas.exportar	Exportar escalas	Exportar escalas em PDF	escalas	t	t	2026-07-22 01:39:45.558487+00	\N	seed	\N	Gestão do Setor
b83b7094-a472-2b4d-b493-f30bd299b6e7	escalas.listar	Listar escalas	Visualizar escalas dos setores autorizados	escalas	t	t	2026-07-22 01:39:45.558359+00	\N	seed	\N	Gestão do Setor
8e1400bc-489e-aa43-8070-36a4f8cb73f9	escalas.editar	Editar escalas	Alterar rascunhos e escalas finalizadas	escalas	t	t	2026-07-22 01:39:45.558454+00	2026-07-23 02:34:06.469825+00	seed	seed	Gestão do Setor
94f93690-dcd9-0f49-ac7d-a2b8f83b5fbe	escalas.publicar	Publicar escalas	Publicar escalas finalizadas	escalas	t	t	2026-07-22 01:39:45.558473+00	2026-07-23 02:34:06.483501+00	seed	seed	Gestão do Setor
0859dc8e-8b11-9b45-8477-7e7e4cb3a806	escalas.solicitar_devolucao	Solicitar devolução de escala	Solicitar devolução de escala publicada	escalas	t	t	2026-07-23 02:34:06.483986+00	\N	seed	\N	Gestão do Setor
93ee4437-ec79-3c4c-b7ee-6b2395977320	escalas.excluir	Excluir escalas	Excluir escalas em rascunho ou finalizadas	escalas	t	t	2026-07-23 02:34:06.483535+00	\N	seed	\N	Gestão do Setor
a1975da1-8acb-b547-b460-c771e9728617	escalas.finalizar	Finalizar escalas	Finalizar montagem da escala	escalas	t	t	2026-07-23 02:34:06.470438+00	\N	seed	\N	Gestão do Setor
d2c43544-1ee6-004c-be01-095e928922a9	escalas.devolver	Aprovar devolução de escala	Aprovar ou recusar devolução de escalas	escalas	t	t	2026-07-23 02:34:06.484024+00	\N	seed	\N	Gestão Institucional
80e1bedf-f79a-3347-b801-ea5da24bdeca	afastamentos.criar	Criar afastamentos	Cadastrar afastamentos	afastamentos	t	t	2026-07-23 03:22:42.50306+00	\N	seed	\N	Gestão do Setor
c14d090b-2236-914b-b9ea-407bc2e4453d	afastamentos.excluir	Excluir afastamentos	Remover afastamentos	afastamentos	t	t	2026-07-23 03:22:42.503505+00	\N	seed	\N	Gestão do Setor
c43359d1-fd50-6048-b7ca-43b7da281f90	afastamentos.listar	Listar afastamentos	Visualizar afastamentos dos servidores	afastamentos	t	t	2026-07-23 03:22:42.48994+00	\N	seed	\N	Gestão do Setor
f7cab90b-b061-964e-adae-6e1f5f7a63c7	afastamentos.editar	Editar afastamentos	Alterar afastamentos	afastamentos	t	t	2026-07-23 03:22:42.503476+00	\N	seed	\N	Gestão do Setor
b67e8e61-45f9-7646-b2f8-6ad79b590018	servidores.excluir	Excluir servidores	Excluir servidores sem vínculos bloqueantes	servidores	t	t	2026-07-24 02:35:50.122281+00	\N	seed	\N	Gestão Institucional
\.


--
-- Data for Name: Servidor; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Servidor" ("Id", "Nome", "Matricula", "Cpf", "Email", "Telefone", "SetorId", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "CargoId", "Status", "DataNascimento") FROM stdin;
769e902a-81aa-460e-b584-24fe26c61b29	Sarah Oliveira	123.123-3	31327795043	mankoubenkyou@gmail.com	(84) 98702-4580	f439d924-ba08-487e-a284-0ab578b7ee06	2026-07-23 23:26:39.878877+00	\N	vitorlopes	\N	174cf381-fb97-4301-aa54-227cbc179b23	Ativo	1980-10-07
cedbbee0-8760-46b9-9a5c-07b6283faf8b	Sarah Maria de Oliveira Ribeiro	123.321-3	06802008432		\N	11111111-1111-1111-1111-111111111111	2026-07-24 02:01:42.151225+00	\N	vitorlopes	\N	661d799f-4307-463e-9215-dd84698c5d98	Ativo	1998-04-06
22222222-2222-2222-2222-222222222222	Vitor Lopes	00.000-1	00000000000	vitorlopes@pci.rn.gov.br	\N	11111111-1111-1111-1111-111111111111	2026-07-21 01:13:10.517544+00	\N	seed	\N	174cf381-fb97-4301-aa54-227cbc179b23	Ativo	1990-01-01
6de7afd2-510c-483a-94db-fa716e74138c	Rosângela D'Avilla	220.547-5	50086859021	davillawitte@gmail.com	(84) 98702-4580	11111111-1111-1111-1111-111111111111	2026-07-22 01:44:25.21642+00	2026-07-22 01:59:17.895922+00	vitorlopes	vitorlopes	174cf381-fb97-4301-aa54-227cbc179b23	Ativo	1995-09-21
b2536338-d43c-431f-b516-4cd524c3d78f	Alyson Costa	123.123-2	00378107070	mankoubenkyou@gmail.com	(84) 98202-4580	dcabe2de-a1c5-49e8-b10e-3a20fa740f78	2026-07-23 11:34:23.623858+00	\N	vitorlopes	\N	76470c0b-7c46-456f-8c10-053e75a92358	Ativo	2003-07-03
c3baf373-ff8b-4635-af22-9934234cbd92	Ricardo Oliveira	123.412-3	10393306496	mankoubenkyou@gmail.com	(84) 98702-4580	dcabe2de-a1c5-49e8-b10e-3a20fa740f78	2026-07-21 17:44:12.175597+00	2026-07-23 20:29:08.493964+00	vitorlopes	vitorlopes	9b80a4b6-4a6c-43c1-a08c-a30e1814644b	Ativo	1980-07-10
\.


--
-- Data for Name: Setor; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Setor" ("Id", "Nome", "Sigla", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "NucleoId", "Resumo") FROM stdin;
f439d924-ba08-487e-a284-0ab578b7ee06	Toxicologia Forense	STF	2026-07-21 22:40:03.911745+00	2026-07-21 22:46:08.232611+00	vitorlopes	vitorlopes	012c969b-af18-4af8-8d21-ebba48ac59a5	Faz exames de Toxicologia
dcabe2de-a1c5-49e8-b10e-3a20fa740f78	Quimica forense	QUIMICA	2026-07-21 22:46:56.210981+00	\N	vitorlopes	\N	012c969b-af18-4af8-8d21-ebba48ac59a5	\N
a527668e-0520-4c66-a84b-98c3a2495218	Recebimentos de vestigios	RV	2026-07-23 23:28:08.508616+00	\N	vitorlopes	\N	52213705-b435-45f4-a4a7-d7d81a68729e	\N
392cac0a-c111-4733-970a-8df4d8d76531	testando	testandot	2026-07-24 02:03:09.490575+00	\N	vitorlopes	\N	7f1dfbfe-c74c-46c6-876f-ca44059ec84a	tiesitesioht
11111111-1111-1111-1111-111111111111	Direção do Instituto de Criminalística	Direção IC	2026-07-21 01:13:10.481777+00	2026-07-24 02:49:06.353583+00	seed	seed	\N	Direção geral do Instituto de Criminalística
\.


--
-- Data for Name: SetorChefia; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SetorChefia" ("SetorId", "TipoChefia", "ServidorId") FROM stdin;
11111111-1111-1111-1111-111111111111	Diretor	22222222-2222-2222-2222-222222222222
dcabe2de-a1c5-49e8-b10e-3a20fa740f78	ChefiaImediata	c3baf373-ff8b-4635-af22-9934234cbd92
392cac0a-c111-4733-970a-8df4d8d76531	ChefiaImediata	cedbbee0-8760-46b9-9a5c-07b6283faf8b
392cac0a-c111-4733-970a-8df4d8d76531	ChefiaSubstituta	769e902a-81aa-460e-b584-24fe26c61b29
\.


--
-- Data for Name: SolicitacaoDevolucaoEscala; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SolicitacaoDevolucaoEscala" ("Id", "EscalaId", "SolicitanteUsuarioId", "Justificativa", "Status", "RespondidoPor", "RespostaEm", "ObservacaoResposta", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy") FROM stdin;
88c67eec-bc3a-44ce-84c9-f9d01243c1f1	9f8fc8cc-819c-4c4f-8535-20e84d5385fd	aafdd118-2ec7-4a62-89a4-a09a3a349b7e	Errei a data do servidor Alyson Costa	Aprovada	vitorlopes	2026-07-24 00:35:16.026455+00	\N	2026-07-23 23:48:23.058406+00	2026-07-24 00:35:16.026492+00	123	vitorlopes
\.


--
-- Data for Name: TipoOcorrencia; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TipoOcorrencia" ("Codigo", "Nome", "HorasPadrao", "Categoria", "Ativo") FROM stdin;
CF	Chefia Núcleo 12h	12.00	Trabalho	t
D	Descanso	\N	Folga	t
F	Feriado	\N	Folga	t
FR	Férias	\N	Afastamento	t
LM	Licença Médica	\N	Afastamento	t
LO	Licença Outros	\N	Afastamento	t
LP	Licença Prêmio	\N	Afastamento	t
M	Manhã 6h	6.00	Trabalho	t
PD	Plantão Diurno 12h	12.00	Trabalho	t
PN	Plantão Noturno 12h	12.00	Trabalho	t
PT	Plantão 24h	24.00	Trabalho	t
R	Remoção	\N	Outro	t
T	Tarde 6h	6.00	Trabalho	t
TL6	Teletrabalho 6h	6.00	Trabalho	t
\.


--
-- Data for Name: Usuario; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Usuario" ("Id", "ServidorId", "Login", "SenhaHash", "UltimoLogin", "Bloqueado", "Ativo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "DeveAlterarSenha") FROM stdin;
33333333-3333-3333-3333-333333333333	22222222-2222-2222-2222-222222222222	vitorlopes	AQAAAAIAAYagAAAAEDhOmfiVD5s24iNZRW5iHOqtWQ446UrRssSQxJuaYn8Ovi/SPP2j5EPR0zZFWSl4ew==	2026-07-24 01:46:03.905201+00	f	t	2026-07-21 01:13:10.622503+00	2026-07-24 01:46:03.905222+00	seed	vitorlopes	f
aafdd118-2ec7-4a62-89a4-a09a3a349b7e	c3baf373-ff8b-4635-af22-9934234cbd92	123	AQAAAAIAAYagAAAAEE61HypeTM6T4tG9PyeUSUpyhUvJrwZOyKCB3jeECQNHV/7jH1eLg9Sao0jMvIyd8g==	2026-07-24 02:05:27.179775+00	f	t	2026-07-21 17:44:23.696212+00	2026-07-24 02:05:27.179776+00	vitorlopes	123	f
\.


--
-- Data for Name: UsuarioPerfil; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."UsuarioPerfil" ("UsuarioId", "PerfilId") FROM stdin;
33333333-3333-3333-3333-333333333333	aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
aafdd118-2ec7-4a62-89a4-a09a3a349b7e	bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb
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
20260722030240_AddUsuarioDeveAlterarSenha	10.0.0
20260722033101_AddPadraoEscalaAndCicloContinuo	10.0.0
20260723015742_EscalaFinalizadaDevolucaoAndPermissaoArea	10.0.0
20260723030641_AddAfastamento	10.0.0
20260723172507_AddAfastamentoSei	10.0.0
\.


--
-- Name: Afastamento PK_Afastamento; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Afastamento"
    ADD CONSTRAINT "PK_Afastamento" PRIMARY KEY ("Id");


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
-- Name: PadraoEscala PK_PadraoEscala; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PadraoEscala"
    ADD CONSTRAINT "PK_PadraoEscala" PRIMARY KEY ("Id");


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
-- Name: SolicitacaoDevolucaoEscala PK_SolicitacaoDevolucaoEscala; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SolicitacaoDevolucaoEscala"
    ADD CONSTRAINT "PK_SolicitacaoDevolucaoEscala" PRIMARY KEY ("Id");


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
-- Name: IX_Afastamento_DataInicio_DataFim; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Afastamento_DataInicio_DataFim" ON public."Afastamento" USING btree ("DataInicio", "DataFim");


--
-- Name: IX_Afastamento_ServidorId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Afastamento_ServidorId" ON public."Afastamento" USING btree ("ServidorId");


--
-- Name: IX_Afastamento_TipoOcorrenciaCodigo; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Afastamento_TipoOcorrenciaCodigo" ON public."Afastamento" USING btree ("TipoOcorrenciaCodigo");


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
-- Name: IX_EscalaJornada_PadraoEscalaId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EscalaJornada_PadraoEscalaId" ON public."EscalaJornada" USING btree ("PadraoEscalaId");


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
-- Name: IX_PadraoEscala_Codigo; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_PadraoEscala_Codigo" ON public."PadraoEscala" USING btree ("Codigo");


--
-- Name: IX_PadraoEscala_SetorId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PadraoEscala_SetorId" ON public."PadraoEscala" USING btree ("SetorId");


--
-- Name: IX_PadraoEscala_TipoFuncionamento; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PadraoEscala_TipoFuncionamento" ON public."PadraoEscala" USING btree ("TipoFuncionamento");


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
-- Name: IX_Permissao_Area; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Permissao_Area" ON public."Permissao" USING btree ("Area");


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
-- Name: IX_SolicitacaoDevolucaoEscala_EscalaId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SolicitacaoDevolucaoEscala_EscalaId" ON public."SolicitacaoDevolucaoEscala" USING btree ("EscalaId");


--
-- Name: IX_SolicitacaoDevolucaoEscala_EscalaId_Status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SolicitacaoDevolucaoEscala_EscalaId_Status" ON public."SolicitacaoDevolucaoEscala" USING btree ("EscalaId", "Status");


--
-- Name: IX_SolicitacaoDevolucaoEscala_SolicitanteUsuarioId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SolicitacaoDevolucaoEscala_SolicitanteUsuarioId" ON public."SolicitacaoDevolucaoEscala" USING btree ("SolicitanteUsuarioId");


--
-- Name: IX_SolicitacaoDevolucaoEscala_Status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SolicitacaoDevolucaoEscala_Status" ON public."SolicitacaoDevolucaoEscala" USING btree ("Status");


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
-- Name: Afastamento FK_Afastamento_Servidor_ServidorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Afastamento"
    ADD CONSTRAINT "FK_Afastamento_Servidor_ServidorId" FOREIGN KEY ("ServidorId") REFERENCES public."Servidor"("Id") ON DELETE RESTRICT;


--
-- Name: EscalaJornada FK_EscalaJornada_EscalaServidor_EscalaServidorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaJornada"
    ADD CONSTRAINT "FK_EscalaJornada_EscalaServidor_EscalaServidorId" FOREIGN KEY ("EscalaServidorId") REFERENCES public."EscalaServidor"("Id") ON DELETE CASCADE;


--
-- Name: EscalaJornada FK_EscalaJornada_PadraoEscala_PadraoEscalaId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EscalaJornada"
    ADD CONSTRAINT "FK_EscalaJornada_PadraoEscala_PadraoEscalaId" FOREIGN KEY ("PadraoEscalaId") REFERENCES public."PadraoEscala"("Id") ON DELETE SET NULL;


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
-- Name: PadraoEscala FK_PadraoEscala_Setor_SetorId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PadraoEscala"
    ADD CONSTRAINT "FK_PadraoEscala_Setor_SetorId" FOREIGN KEY ("SetorId") REFERENCES public."Setor"("Id") ON DELETE RESTRICT;


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
-- Name: SolicitacaoDevolucaoEscala FK_SolicitacaoDevolucaoEscala_Escala_EscalaId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SolicitacaoDevolucaoEscala"
    ADD CONSTRAINT "FK_SolicitacaoDevolucaoEscala_Escala_EscalaId" FOREIGN KEY ("EscalaId") REFERENCES public."Escala"("Id") ON DELETE CASCADE;


--
-- Name: SolicitacaoDevolucaoEscala FK_SolicitacaoDevolucaoEscala_Usuario_SolicitanteUsuarioId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SolicitacaoDevolucaoEscala"
    ADD CONSTRAINT "FK_SolicitacaoDevolucaoEscala_Usuario_SolicitanteUsuarioId" FOREIGN KEY ("SolicitanteUsuarioId") REFERENCES public."Usuario"("Id") ON DELETE RESTRICT;


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

\unrestrict q3hbgGsEVxEbh36HfPL72yPZkyPFz1Dtm6xIkGaiDczIR7jZj2m4o9HV0GISnzh

