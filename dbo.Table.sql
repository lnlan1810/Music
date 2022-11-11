CREATE TABLE [dbo].[Table] (
    [Id]       INT           IDENTITY (1, 1) NOT NULL,
    [Login]    VARCHAR (MAX) NOT NULL,
    [Password] VARCHAR (MAX) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);

