CREATE TYPE erabiltzailemota AS ENUM ('admin', 'user');

CREATE TABLE erabiltzaileak (
    username character(50) NOT NULL,
    elo integer,
    mota erabiltzailemota NOT NULL,
    avatar character(255),
    irabazita integer,
    galduta integer,
    enpate integer,
    password character(255) NOT NULL
);

INSERT INTO erabiltzaileak (username, password, mota) 
VALUES ('admin', 'admin', 'admin');

INSERT INTO erabiltzaileak (username, password, mota) 
VALUES ('user', 'user', 'user');
