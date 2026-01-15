CREATE TYPE erabiltzailemota AS ENUM ('admin', 'user');

CREATE TABLE erabiltzaileak (
    id bigint primary key generated always as identity,
    username text NOT NULL UNIQUE,
    password text NOT NULL,
    elo integer DEFAULT 1000,
    mota erabiltzailemota NOT NULL,
    avatar text
);

INSERT INTO erabiltzaileak (username, password, mota) 
VALUES ('admin', 'admin', 'admin');

INSERT INTO erabiltzaileak (username, password, mota) 
VALUES ('user', 'user', 'user');

CREATE TABLE partidak (
    id bigint primary key generated always as identity,
    player1_id bigint NOT NULL,
    player2_id bigint NOT NULL,
    winner_id bigint,
    FOREIGN KEY (player1_id) REFERENCES erabiltzaileak(id),
    FOREIGN KEY (player2_id) REFERENCES erabiltzaileak(id),
    FOREIGN KEY (winner_id) REFERENCES erabiltzaileak(id)
);

CREATE TABLE partida_kola (
    id bigint primary key generated always as identity,
    user_id bigint NOT NULL,
    FOREIGN KEY (user_id) REFERENCES erabiltzaileak(id)
);