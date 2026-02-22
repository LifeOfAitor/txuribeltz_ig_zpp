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
('aitor', 'aitor123', 'user', 1200),
('ikerg', 'ikerG123', 'user', 1100),
('unai', 'unai123', 'user', 1000),
('jaime', 'jaime123', 'user', 1300),
('danel', 'danel123', 'user', 1200),
('julen', 'julen123', 'user', 1000),
('xabier', 'xabier123', 'user', 1100),
('ibai', 'ibai123', 'user', 1200),
('ivan', 'ivan123', 'user', 1000),
('iker', 'iker123', 'user', 1100),
('bittor', 'bittor123', 'user', 1400),
('mikel', 'mikel123', 'user', 1000),
('jon', 'jon123', 'user', 800),
('aritz', 'aritz123', 'user', 1400),
('extra', 'extra123', 'user', 500);

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