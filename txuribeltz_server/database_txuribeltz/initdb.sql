CREATE TYPE erabiltzailemota AS ENUM ('admin', 'user');

CREATE TABLE erabiltzaileak (
    id bigint primary key generated always as identity,
    username text NOT NULL UNIQUE,
    password text NOT NULL,
    elo integer DEFAULT 1000,
    mota erabiltzailemota NOT NULL,
    avatar text
);

CREATE TABLE partidak (
    id bigint primary key generated always as identity,
    player1_id bigint NOT NULL,
    player2_id bigint NOT NULL,
    winner_id bigint NOT NULL,
    played_at date  NOT NULL,
    FOREIGN KEY (player1_id) REFERENCES erabiltzaileak(id),
    FOREIGN KEY (player2_id) REFERENCES erabiltzaileak(id),
    FOREIGN KEY (winner_id) REFERENCES erabiltzaileak(id)
);

CREATE TABLE partida_kola (
    id bigint primary key generated always as identity,
    user_id bigint NOT NULL,
    FOREIGN KEY (user_id) REFERENCES erabiltzaileak(id)
);

INSERT INTO erabiltzaileak (username, password, mota) 
VALUES ('admin', 'admin', 'admin');

INSERT INTO erabiltzaileak (username, password, mota) 
VALUES ('user', 'user', 'user');

INSERT INTO erabiltzaileak (username, password, mota, elo) VALUES
('alice', 'alice123', 'user', 1200),
('bob', 'bob123', 'user', 1100),
('carol', 'carol123', 'user', 1050),
('dave', 'dave123', 'user', 1300),
('eve', 'eve123', 'user', 1250),
('frank', 'frank123', 'user', 1000),
('grace', 'grace123', 'user', 1150),
('heidi', 'heidi123', 'user', 1080),
('ivan', 'ivan123', 'user', 1020),
('judy', 'judy123', 'user', 1180),
('mallory', 'mallory123', 'user', 1350),
('oscar', 'oscar123', 'user', 980),
('peggy', 'peggy123', 'user', 1120),
('trent', 'trent123', 'user', 1400),
('victor', 'victor123', 'user', 1070);

INSERT INTO partidak (player1_id, player2_id, winner_id, played_at) VALUES
(3, 4, 3, current_date - 3),
(5, 6, 6, current_date - 5),
(7, 8, 7, current_date - 7),
(9, 10, 10, current_date - 9),
(11,12, 11, current_date - 11),
(13,14, 14, current_date - 13),
(15,16, 15, current_date - 15),
(17,3, 17, current_date - 18),
(4, 5, 5, current_date - 20),
(6, 7, 6, current_date - 22),
(8, 9, 8, current_date - 25),
(10,11, 10, current_date - 28),
(12,13, 13, current_date - 30),
(14,15, 15, current_date - 33),
(16,17, 16, current_date - 36),
(3, 6, 3, current_date - 40),
(4, 7, 7, current_date - 45),
(5, 8, 5, current_date - 50),
(9, 12, 12, current_date - 55),
(11,14, 11, current_date - 58);