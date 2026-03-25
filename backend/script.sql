CREATE TABLE "AgentTypes" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "DefaultSystemPrompt" text,
    "DefaultTemperature" double precision NOT NULL,
    "DefaultMaxTokens" integer NOT NULL,
    "Icon" character varying(50),
    "SortOrder" integer NOT NULL,
    "IsSystem" boolean NOT NULL,
    "UserId" character varying(100),
    "IsEnabled" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_AgentTypes" PRIMARY KEY ("Id")
);


CREATE TABLE "Collaborations" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000),
    "Path" character varying(500),
    "GitRepositoryUrl" character varying(500),
    "GitBranch" character varying(100),
    "GitUsername" character varying(100),
    "GitEmail" character varying(100),
    "GitAccessToken" character varying(500),
    "Status" integer NOT NULL,
    "UserId" character varying(100),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Collaborations" PRIMARY KEY ("Id")
);


CREATE TABLE "LLMConfigs" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Provider" character varying(50) NOT NULL,
    "ApiKey" text NOT NULL,
    "Endpoint" character varying(500),
    "IsDefault" boolean NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "UserId" character varying(100),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_LLMConfigs" PRIMARY KEY ("Id")
);


CREATE TABLE "OperationLogs" (
    "Id" uuid NOT NULL,
    "Operation" character varying(50) NOT NULL,
    "Module" character varying(50) NOT NULL,
    "Description" character varying(500),
    "UserId" character varying(100),
    "UserName" character varying(100),
    "IpAddress" character varying(50),
    "RequestData" text,
    "ResponseData" text,
    "IsSuccess" boolean NOT NULL,
    "ErrorMessage" text,
    "Duration" bigint,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_OperationLogs" PRIMARY KEY ("Id")
);


CREATE TABLE "RagDocuments" (
    "Id" uuid NOT NULL,
    "FileName" character varying(500) NOT NULL,
    "OriginalFileName" character varying(500),
    "FilePath" character varying(1000),
    "FileType" character varying(50),
    "FileSize" bigint NOT NULL,
    "ContentHash" character varying(100),
    "SplitMethod" character varying(50),
    "ChunkSize" integer,
    "ChunkOverlap" integer,
    "ChunkCount" integer NOT NULL,
    "Status" integer NOT NULL,
    "ErrorMessage" text,
    "UserId" character varying(100),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_RagDocuments" PRIMARY KEY ("Id")
);


CREATE TABLE "SystemConfigs" (
    "Id" uuid NOT NULL,
    "Key" character varying(100) NOT NULL,
    "Value" text NOT NULL,
    "Description" character varying(500),
    "Group" character varying(50),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_SystemConfigs" PRIMARY KEY ("Id")
);


CREATE TABLE "SystemLogs" (
    "Id" uuid NOT NULL,
    "Level" character varying(20) NOT NULL,
    "Category" character varying(500),
    "Message" text NOT NULL,
    "Exception" text,
    "StackTrace" text,
    "RequestPath" character varying(500),
    "RequestMethod" character varying(10),
    "UserId" character varying(100),
    "UserName" character varying(100),
    "IpAddress" character varying(50),
    "ExtraData" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_SystemLogs" PRIMARY KEY ("Id")
);


CREATE TABLE "Users" (
    "Id" text NOT NULL,
    "Username" character varying(50) NOT NULL,
    "Email" character varying(100) NOT NULL,
    "PasswordHash" text NOT NULL,
    "Avatar" character varying(500),
    "Role" character varying(20) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);


CREATE TABLE "CollaborationTasks" (
    "Id" uuid NOT NULL,
    "CollaborationId" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Description" character varying(2000),
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    CONSTRAINT "PK_CollaborationTasks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CollaborationTasks_Collaborations_CollaborationId" FOREIGN KEY ("CollaborationId") REFERENCES "Collaborations" ("Id") ON DELETE CASCADE
);


CREATE TABLE "LLMModelConfigs" (
    "Id" uuid NOT NULL,
    "LLMConfigId" uuid NOT NULL,
    "ModelName" character varying(100) NOT NULL,
    "DisplayName" character varying(100),
    "Temperature" double precision NOT NULL,
    "MaxTokens" integer NOT NULL,
    "ContextWindow" integer NOT NULL,
    "TopP" double precision,
    "FrequencyPenalty" double precision,
    "PresencePenalty" double precision,
    "StopSequences" text,
    "IsDefault" boolean NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "SortOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_LLMModelConfigs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LLMModelConfigs_LLMConfigs_LLMConfigId" FOREIGN KEY ("LLMConfigId") REFERENCES "LLMConfigs" ("Id") ON DELETE CASCADE
);


CREATE TABLE "RagDocumentChunks" (
    "Id" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "Content" text NOT NULL,
    "ChunkIndex" integer NOT NULL,
    "VectorId" character varying(100),
    "Metadata" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_RagDocumentChunks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RagDocumentChunks_RagDocuments_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "RagDocuments" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Agents" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "Type" character varying(50) NOT NULL,
    "LLMConfigId" uuid,
    "LLMModelConfigId" uuid,
    "Configuration" text NOT NULL,
    "Avatar" character varying(500),
    "UserId" text NOT NULL,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "LastActiveAt" timestamp with time zone,
    CONSTRAINT "PK_Agents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Agents_LLMConfigs_LLMConfigId" FOREIGN KEY ("LLMConfigId") REFERENCES "LLMConfigs" ("Id"),
    CONSTRAINT "FK_Agents_LLMModelConfigs_LLMModelConfigId" FOREIGN KEY ("LLMModelConfigId") REFERENCES "LLMModelConfigs" ("Id"),
    CONSTRAINT "FK_Agents_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "LLMTestRecords" (
    "Id" uuid NOT NULL,
    "LLMConfigId" uuid NOT NULL,
    "LLMModelConfigId" uuid,
    "Provider" character varying(50) NOT NULL,
    "ModelName" character varying(100),
    "IsSuccess" boolean NOT NULL,
    "Message" character varying(500),
    "LatencyMs" integer NOT NULL,
    "TestedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_LLMTestRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LLMTestRecords_LLMConfigs_LLMConfigId" FOREIGN KEY ("LLMConfigId") REFERENCES "LLMConfigs" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LLMTestRecords_LLMModelConfigs_LLMModelConfigId" FOREIGN KEY ("LLMModelConfigId") REFERENCES "LLMModelConfigs" ("Id") ON DELETE SET NULL
);


CREATE TABLE "AgentMessages" (
    "Id" uuid NOT NULL,
    "FromAgentId" uuid NOT NULL,
    "CollaborationId" uuid,
    "ToAgentId" uuid NOT NULL,
    "Content" character varying(5000) NOT NULL,
    "Type" integer NOT NULL,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ProcessedAt" timestamp with time zone,
    CONSTRAINT "PK_AgentMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AgentMessages_Agents_FromAgentId" FOREIGN KEY ("FromAgentId") REFERENCES "Agents" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AgentMessages_Agents_ToAgentId" FOREIGN KEY ("ToAgentId") REFERENCES "Agents" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AgentMessages_Collaborations_CollaborationId" FOREIGN KEY ("CollaborationId") REFERENCES "Collaborations" ("Id")
);


CREATE TABLE "CollaborationAgents" (
    "Id" uuid NOT NULL,
    "CollaborationId" uuid NOT NULL,
    "AgentId" uuid NOT NULL,
    "Role" character varying(100),
    "JoinedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_CollaborationAgents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CollaborationAgents_Agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "Agents" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CollaborationAgents_Collaborations_CollaborationId" FOREIGN KEY ("CollaborationId") REFERENCES "Collaborations" ("Id") ON DELETE CASCADE
);


CREATE INDEX "IX_AgentMessages_CollaborationId" ON "AgentMessages" ("CollaborationId");


CREATE INDEX "IX_AgentMessages_FromAgentId" ON "AgentMessages" ("FromAgentId");


CREATE INDEX "IX_AgentMessages_ToAgentId" ON "AgentMessages" ("ToAgentId");


CREATE INDEX "IX_Agents_LLMConfigId" ON "Agents" ("LLMConfigId");


CREATE INDEX "IX_Agents_LLMModelConfigId" ON "Agents" ("LLMModelConfigId");


CREATE INDEX "IX_Agents_Name" ON "Agents" ("Name");


CREATE INDEX "IX_Agents_UserId" ON "Agents" ("UserId");


CREATE UNIQUE INDEX "IX_AgentTypes_Code" ON "AgentTypes" ("Code");


CREATE INDEX "IX_CollaborationAgents_AgentId" ON "CollaborationAgents" ("AgentId");


CREATE UNIQUE INDEX "IX_CollaborationAgents_CollaborationId_AgentId" ON "CollaborationAgents" ("CollaborationId", "AgentId");


CREATE INDEX "IX_CollaborationTasks_CollaborationId" ON "CollaborationTasks" ("CollaborationId");


CREATE INDEX "IX_LLMConfigs_Name" ON "LLMConfigs" ("Name");


CREATE INDEX "IX_LLMConfigs_Provider" ON "LLMConfigs" ("Provider");


CREATE INDEX "IX_LLMModelConfigs_LLMConfigId" ON "LLMModelConfigs" ("LLMConfigId");


CREATE INDEX "IX_LLMModelConfigs_ModelName" ON "LLMModelConfigs" ("ModelName");


CREATE INDEX "IX_LLMTestRecords_LLMConfigId" ON "LLMTestRecords" ("LLMConfigId");


CREATE INDEX "IX_LLMTestRecords_LLMModelConfigId" ON "LLMTestRecords" ("LLMModelConfigId");


CREATE INDEX "IX_LLMTestRecords_TestedAt" ON "LLMTestRecords" ("TestedAt");


CREATE INDEX "IX_OperationLogs_CreatedAt" ON "OperationLogs" ("CreatedAt");


CREATE INDEX "IX_OperationLogs_UserId" ON "OperationLogs" ("UserId");


CREATE INDEX "IX_RagDocumentChunks_DocumentId" ON "RagDocumentChunks" ("DocumentId");


CREATE INDEX "IX_RagDocuments_FileName" ON "RagDocuments" ("FileName");


CREATE INDEX "IX_RagDocuments_Status" ON "RagDocuments" ("Status");


CREATE UNIQUE INDEX "IX_SystemConfigs_Key" ON "SystemConfigs" ("Key");


CREATE INDEX "IX_SystemLogs_Category" ON "SystemLogs" ("Category");


CREATE INDEX "IX_SystemLogs_CreatedAt" ON "SystemLogs" ("CreatedAt");


CREATE INDEX "IX_SystemLogs_Level" ON "SystemLogs" ("Level");


CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");


CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");


