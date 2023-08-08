﻿import { QueryBuilder } from "../engine/query/QueryBuilder";
import { State } from "../models/State";

export interface IQuery<T, TKey> {
    get(key: TKey): Promise<T>;
    exist(key: TKey): Promise<State<T, TKey>>;
    query(): QueryBuilder<T, TKey>;
}
