import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';

interface GraphQLError {
  message: string;
}

interface GraphQLResponse<T> {
  data?: T;
  errors?: GraphQLError[];
}

/**
 * Minimal GraphQL transport over HttpClient. Orchard returns HTTP 200 with an `errors` array for
 * query-level failures, so surfacing those explicitly is what keeps a bad query from silently
 * looking like empty content.
 */
@Injectable({ providedIn: 'root' })
export class GraphQLClient {
  private readonly http = inject(HttpClient);

  query<T>(query: string, variables?: Record<string, unknown>): Observable<T> {
    return this.http
      .post<GraphQLResponse<T>>(environment.graphqlUrl, { query, variables })
      .pipe(
        map((response) => {
          if (response.errors?.length) {
            throw new Error(response.errors.map((e) => e.message).join('; '));
          }
          return response.data as T;
        }),
      );
  }
}
